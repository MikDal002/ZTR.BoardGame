using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;

[GitHubActions(
    "Create Velopack Release",
    GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { MasterBranch, MainBranch, DevelopBranch },
    PublishArtifacts = true,
    FetchDepth = 0,
    InvokedTargets = new[] { nameof(UnitTests), nameof(PublishToGitHubWithVelopack) },
    CacheKeyFiles = new[] { "**/global.json", "**/*.csproj" },
    CacheIncludePatterns = new[] { ".nuke/temp", "~/.nuget/packages" },
    EnableGitHubToken = true
)]

partial class Build
{
    const string MainBranch = "main";
    const string MasterBranch = "master";
    const string DevelopBranch = "develop";

    [Parameter][Secret] readonly string GitHubToken;
    [Parameter] string GitHubBrowseUrl => GitRepository.ToString();

    Target DownloadGithubRelease => _ => _
        .Before(PackWithVelopack)
        .After(CleanVelopack)
        .Requires(() => GitHubToken)
        .OnlyWhenStatic(() => GitRepository.IsGitHubRepository())
        .Executes(() =>
        {
            var isPrerelease = !GitVersion.PreReleaseLabel.IsNullOrWhiteSpace();

            var arguments =
                "download github" +
                $" --repoUrl {GitHubBrowseUrl}" +
                $" --token {GitHubToken}" +
                $" --outputDir {VelopackPublish}" +
                $" --channel {Channel}";

            if (isPrerelease)
            {
                arguments = arguments.Append(" --pre");
            }

            Vpk.Invoke(arguments);
        });

    Target PublishToGitHubWithVelopack => _ => _
        .DependsOn(PackWithVelopack)
        .DependsOn(DownloadGithubRelease)
        .Requires(() => GitHubToken)
        .OnlyWhenStatic(() => GitRepository.IsGitHubRepository())
        .Executes(() =>
        {
            var isPrerelease = !GitVersion.PreReleaseLabel.IsNullOrWhiteSpace();
            var releaseName = GitVersion.FullSemVer;
            var tag = GitVersion.FullSemVer;
            var arguments = "upload github" +
                            $" --outputDir {VelopackPublish}" +
                            $" --channel {Channel}" +
                            $" --repoUrl {GitHubBrowseUrl}" +
                            $" --token {GitHubToken}" +
                            $" --publish" +
                            $" --merge" +
                            $" --releaseName {releaseName}" +
                            $" --tag {tag}" +
                            $" --targetCommitish {GitVersion.Sha}";

            if (isPrerelease)
            {
                arguments = arguments.Append(" --pre");
            }

            Vpk.Invoke(arguments);
        });
}

