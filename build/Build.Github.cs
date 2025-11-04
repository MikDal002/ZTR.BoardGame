using Nuke.Common.CI.GitHubActions;

[GitHubActions("For each PR",
    GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest, OnPullRequestBranches = new[]
    {
        DevelopBranch, MainBranch, MasterBranch, "feature/*"
    },
    PublishArtifacts = false,
    FetchDepth = 0,
    InvokedTargets = new[] { nameof(Tests) },
    CacheKeyFiles = new[] { "**/global.json", "**/*.csproj" },
    CacheIncludePatterns = new[] { ".nuke/temp", "~/.nuget/packages" },
    EnableGitHubToken = true
)]

public partial class Build
{
}
