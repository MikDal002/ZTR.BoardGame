#nullable enable
using Nuke.Common;
using Nuke.Common.Utilities;
using Serilog;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZtrTemplates.Configuration.Shared;

public partial class Build
{
    [Parameter("Base URL for Velopack updates served via the website")]
    readonly string UpdateBaseUrl = "https://frog02-20366.wykr.es";

    Target ConfigureAppSettings => _ => _
        .DependsOn(Clean)
        .Before(Compile)
        .Unlisted()
        .Executes(() =>
        {
            var appSettingsPath = ProjectToPublish.Directory / "appsettings.json";
            var (determinedUpdateUrl, determinedUseGitHubSource, determinedFetchPrereleases) = GetTargetSpecificUpdateOptions();

            if (determinedUpdateUrl == null)
            {
                Log.Warning($"No specific update configuration determined for current execution.");
                return;
            }

            Log.Information($"Attempting to configure update settings in '{appSettingsPath}'. Url: '{determinedUpdateUrl}', UseGitHub: {determinedUseGitHubSource}, FetchPrereleases: {determinedFetchPrereleases}");

            JsonNode? rootNode;

            if (File.Exists(appSettingsPath))
            {
                using var fileStream = File.OpenRead(appSettingsPath);
                try
                {
                    rootNode = JsonNode.Parse(fileStream);
                    Log.Information($"Read existing settings from '{appSettingsPath}'.");
                }
                catch (JsonException ex)
                {
                    Log.Warning($"Failed to parse existing appsettings.json at '{appSettingsPath}'. A new file will be created. Error: {ex.Message}");
                    rootNode = null;
                }
            }
            else
            {
                Log.Information($"File '{appSettingsPath}' not found. Creating a new settings file.");
                rootNode = null;
            }

            rootNode ??= new JsonObject();

            if (rootNode[nameof(UpdateOptions)] is not JsonObject updateOptionsNode)
            {
                updateOptionsNode = new JsonObject();
                rootNode[nameof(UpdateOptions)] = updateOptionsNode;
            }

            if (determinedUpdateUrl != null)
            {
                updateOptionsNode[nameof(UpdateOptions.UpdateUrl)] = determinedUpdateUrl;
            }

            updateOptionsNode[nameof(UpdateOptions.UseGitHubSource)] = determinedUseGitHubSource;
            updateOptionsNode[nameof(UpdateOptions.FetchPrereleases)] = determinedFetchPrereleases;

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(appSettingsPath, rootNode.ToJsonString(jsonOptions));
            Log.Information($"Configured update settings in '{appSettingsPath}'.");
        });

    (string? updateUrl, bool useGitHubSource, bool fetchPrereleases) GetTargetSpecificUpdateOptions()
    {
        Log.Information("Determining target-specific update options...");

        if (ExecutionPlan.Any(t => t.Name == nameof(PublishToGitHubWithVelopack)))
        {
            if (string.IsNullOrWhiteSpace(GitHubBrowseUrl))
            {
                Log.Warning($"{nameof(GitHubBrowseUrl)} is not set for {nameof(PublishToGitHubWithVelopack)}. Cannot determine update URL.");
                return (null, false, false);
            }

            Log.Information($"Target {nameof(PublishToGitHubWithVelopack)} detected. Configuring for GitHub releases.");
            return (GitHubBrowseUrl, true, !GitVersion.PreReleaseLabel.IsNullOrWhiteSpace());
        }
        else if (ExecutionPlan.Any(t => t.Name == nameof(UploadLocalToServer))) // Replace with actual target name if you re-implement it
        {
            Log.Information("Target for custom server detected. Configuring for custom URL.");
            return ($"{UpdateBaseUrl}/{NameProjectDirectoryOnTheServer}/{DirectoryForReleases}", false, false);
        }

        Log.Information("No specific publish target identified for dynamic UpdateOptions configuration.");
        return (null, false, false); // No specific configuration found
    }
}
