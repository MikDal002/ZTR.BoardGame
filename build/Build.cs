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
using System.Linq;

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

    const string NameOfProjectToBePublished = "ZtrTemplates.Console";
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
        .DependsOn(Tests)
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

    Target Tests => _ => _
       .DependsOn(Compile)
       .TriggeredBy(Compile)
       .Executes(() =>
       {
           TestResultDirectory.CreateOrCleanDirectory();
           DotNetTasks.DotNetTest(s => s.SetConfiguration(Configuration)
               .SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
               .SetRuntime(Runtime)
               .SetProjectFile(Solution));
       });

    Target CreateVersionLabel => _ => _
        .TriggeredBy(Publish)
        .OnlyWhenStatic(() => GitRepository.IsOnMainOrMasterBranch() || GitRepository.IsOnDevelopBranch())
        .Executes(() =>
        {
            Log.Information($"Pushing new tag about the version {GitVersion.FullSemVer}");

            if (!IsLocalBuild)
            {
                GitTasks.Git($"config user.email \"build@ourcompany.com\"");
                GitTasks.Git($"config user.name \"Our Company Build\"");
            }

            GitTasks.Git($"tag -a {GitVersion.FullSemVer} -m \"Setting git tag on commit to '{GitVersion.FullSemVer}'\"");

            try
            {
                GitTasks.Git($"push origin refs/tags/{GitVersion.FullSemVer}");
                Log.Information($"Successfully pushed tag {GitVersion.FullSemVer}.");
            }
            catch (ProcessException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("Updates were rejected because the tag already exists"))
            {
                Log.Warning($"Tag {GitVersion.FullSemVer} already exists on remote. Skipping push. Details: {ex.Message}");
            }
        });

}
