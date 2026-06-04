using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.PackWithVelopack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("System to build, deafults to current system. Change if you want to cross compile")]
    readonly OperationSystem OperationSystem = OperationSystem.GetCurrentConfiguration();

    [Parameter("System architecture to build, deafults to current architecture. Change if you want to cross compile")]
    readonly SystemArchitecture SystemArchitecture = SystemArchitecture.GetCurrentConfiguration();

    string Runtime => $"{OperationSystem}-{SystemArchitecture}";

    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
    /// </summary>

    [Solution] readonly Solution Solution;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    const string NameOfProjectToBePublished = "ZtrBoardGame.Console";
    Project ProjectToPublish =>
        Solution.GetProject(NameOfProjectToBePublished)
        ?? Solution.GetAllProjects(NameOfProjectToBePublished).FirstOrDefault()
        ?? Solution.GetAllProjects("*").FirstOrDefault(p => p.Name.Equals(NameOfProjectToBePublished, System.StringComparison.OrdinalIgnoreCase))
        ?? throw new System.Exception($"Project '{NameOfProjectToBePublished}' not found in solution for ProjectToPublish property.");

    AbsolutePath PublishedProjectAsZip =>
        PackagesDirectory / NameOfProjectToBePublished + ".zip";
    AbsolutePath SourceDirectory => RootDirectory / "templates";
    AbsolutePath PublishDirectory => RootDirectory / "output";
    AbsolutePath PackagesDirectory => RootDirectory / "packages";
    AbsolutePath TestResultDirectory => RootDirectory / "testResults";

    static string SanitizeGitTag(string text)
        => Regex.Replace(text, @"[^0-9A-Za-z\.-]", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(10));

    Target Clean => _ => _
        .Before(Restore)
        .DependentFor(Restore)
        .Unlisted()
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            PublishDirectory.DeleteDirectory();
            PackagesDirectory.DeleteDirectory();
            TestResultDirectory.DeleteDirectory();
        });

    Target Format => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetFormat(s => s
                .SetProject(Solution)
                .When(_ => IsServerBuild, s => s)
                .SetVerifyNoChanges(true)
                .SetSeverity("error"));
        });

    Target Restore => _ => _
        .DependsOn(Format)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(ProjectToPublish)
                .SetRuntime(Runtime));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information($"Compiling {GitVersion.SemVer} version");

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(ProjectToPublish)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetRuntime(Runtime)
                .SetTreatWarningsAsErrors(true)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .DependsOn(UnitTests)
        .Executes(() =>
        {
            PublishDirectory.CreateOrCleanDirectory();

            Log.Information("Publishing {projectToPublish} project to {filePath} directory.", ProjectToPublish,
                PublishDirectory);

            DotNetTasks.DotNetPublish(s => s.SetProject(ProjectToPublish)
                .SetConfiguration(Configuration)
                .SetOutput(PublishDirectory)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetSelfContained(true)
                .SetPublishTrimmed(true)
                .SetRuntime(Runtime)
                .SetNoBuild(true)
                );
        });

    Target CreateVersionLabel => _ => _
        .TriggeredBy(Publish)
        .OnlyWhenStatic(() => GitRepository.IsOnMainOrMasterBranch() || GitRepository.IsOnDevelopBranch())
        .Executes(() =>
        {
            var sanitizedVersion = SanitizeGitTag(GitVersion.FullSemVer);
            Log.Information("Pushing new tag about the version {SxanitizedVersion}", sanitizedVersion);

            if (!IsLocalBuild)
            {
                GitTasks.Git($"config user.email \"build@ourcompany.com\"");
                GitTasks.Git($"config user.name \"Our Company Build\"");
            }

            GitTasks.Git($"tag -a {sanitizedVersion} -m \"Setting git tag on commit to '{sanitizedVersion}'\"");

            try
            {
                GitTasks.Git($"push origin refs/tags/{sanitizedVersion}");
                Log.Information("Successfully pushed tag {SanitizedVersion}.", sanitizedVersion);
            }
            catch (ProcessException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("Updates were rejected because the tag already exists"))
            {
                Log.Warning(ex, $"Tag {sanitizedVersion} already exists on remote. Skipping push. Details: {ex.Message}");
            }
        });

}
