using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using System.Linq;

public partial class Build
{
    Project E2ETestsProject =>
        Solution.GetAllProjects("*E2E*").SingleOrDefault()
        ?? throw new System.InvalidOperationException($"Project with E2E in a name,  not found in solution for E2E testing.");

    Target UnitTests => _ => _
        .DependsOn(Compile)
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            TestResultDirectory.CreateOrCleanDirectory();
            var allUnitTestProjects = Solution.GetAllProjects("*Test*").Where(d => d != E2ETestsProject);

            DotNetTasks.DotNetTest(s =>
                s.SetConfiguration(Configuration)
                .SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
                .SetRuntime(Runtime)
                .CombineWith(allUnitTestProjects,
                    (settings, project) => settings.SetProjectFile(project)));
        });

    Target DockerComposeUp => _ => _
        .AssuredAfterFailure()
        .Unlisted()
        .Executes(() => DockerTasks.Docker($"compose --env-file e2etests.env up -d --build", workingDirectory: E2ETestsProject.Directory));

    Target E2ETests => _ => _
        .DependsOn(DockerComposeUp)
        .TriggeredBy(UnitTests)
        .OnlyWhenStatic(() => !(IsServerBuild && OperationSystem == OperationSystem.Windows))
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s.SetConfiguration(Configuration)
                .SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
                .SetRuntime(Runtime)
                .SetProjectFile(E2ETestsProject));
        });

    Target DockerComposeDown => _ => _
        .AssuredAfterFailure()
        .Unlisted()
        .TriggeredBy(E2ETests)
        .Executes(() => DockerTasks.Docker($"compose --env-file e2etests.env down -v", workingDirectory: E2ETestsProject.Directory));
}
