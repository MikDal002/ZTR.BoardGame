using Microsoft.Extensions.Options;
using NuGet.Versioning;
using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ZtrBoardGame.Console;

public interface IUpdateService
{
    SemanticVersion? GetCurrentVersion();
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadAndApplyUpdatesAsync(UpdateInfo newVersion, CancellationToken cancellationToken = default);
}

public class UpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;

    public UpdateService(IOptions<ZtrBoardGame.Configuration.Shared.UpdateOptions> updateOptions)
    {
        var options = updateOptions.Value;
        var url = options.UpdateUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Update URL is not configured in appsettings.json under UpdateOptions section.");
        }

        IUpdateSource source;

        if (options.UseGitHubSource)
        {
            source = new GithubSource(url, null, options.FetchPrereleases);
        }
        else
        {
            source = new SimpleWebSource(url);
        }

        _updateManager = new(source);

    }

    public SemanticVersion? GetCurrentVersion()
    {
        return _updateManager.CurrentVersion;
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        return await _updateManager.CheckForUpdatesAsync();
    }

    public async Task DownloadAndApplyUpdatesAsync(UpdateInfo newVersion, CancellationToken cancellationToken = default)
    {
        // Here we can get the action on progress, we should use it.
        await _updateManager.DownloadUpdatesAsync(newVersion, cancelToken: cancellationToken);
        _updateManager.ApplyUpdatesAndRestart(newVersion);
    }
}
