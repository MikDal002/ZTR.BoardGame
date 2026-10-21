using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
class ConfigureSystemd(ILogger<ConfigureSystemd> logger) : ISystemConfigurer
{
    public string Name => "Systemd Autostart Service";

    private const string ServiceName = "ztrboardgame.service";
    private const string ServicePath = $"/etc/systemd/system/{ServiceName}";

    /// <summary>
    /// Unfortunately Environment.ProcessPath and AppContext.BaseDirectory returns path to the /tmp/.mound.../
    /// directory, which is changed on every run and is not the real path to the executable file. This is because of the
    /// way AppImage works.
    /// </summary>
    private static string GetRealAppPath()
    {

        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
        if (string.IsNullOrEmpty(appImagePath))
        {
            throw new SystemConfigurationException("Cannot get path of APPIMAGE file!");
        }

        return appImagePath;
    }

    public Task<bool> CanConfigureAsync()
        => RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task<bool> IsConfigurationNeededAsync()
    {
        var serviceConfigured = await IsServiceConfiguredAsync();
        logger.LogInformation("System check -> AutoStart Configured: {Service}", serviceConfigured);
        return !serviceConfigured;
    }

    public async Task ConfigureAsync()
        => await InstallSystemdServiceAsync();

    private async Task<bool> IsServiceConfiguredAsync()
    {
        if (!File.Exists(ServicePath))
        {
            return false;
        }

        var currentExePath = GetRealAppPath();

        logger.LogInformation("Checking if service file points to the correct executable path: {Path}", currentExePath);
        try
        {
            var serviceContent = await File.ReadAllTextAsync(ServicePath);
            if (!serviceContent.Contains($"ExecStart={currentExePath}"))
            {
                logger.LogWarning("Service file points to wrong path. Reconfiguration needed.");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read service file.");
            return false;
        }

        var status = await RaspberryPiCommandHelper.RunCommandAsync("systemctl", $"is-enabled {ServiceName}", "Check service status");
        return status.Trim() == "enabled";

    }

    private async Task InstallSystemdServiceAsync()
    {
        logger.LogInformation("Installing systemd service for auto-start for executable file: `{Path} {Args}`", GetRealAppPath(), $"board run");

        var appArguments = $"board run";

        if (!File.Exists(GetRealAppPath()))
        {
            throw new FileNotFoundException($"Executable file not found: {GetRealAppPath()}");
        }

        var workingDirectory = Path.GetDirectoryName(GetRealAppPath());

        var serviceContent = $@"
[Unit]
Description=Ztr Board Game Service
After=network.target multi-user.target

[Service]
Type=simple
User=root
WorkingDirectory={workingDirectory}
ExecStart={GetRealAppPath()} {appArguments}
Restart=always
RestartSec=5
Environment=DOTNET_ROOT=/usr/share/dotnet
StandardOutput=journal+console
StandardError=journal+console

[Install]
WantedBy=multi-user.target
";

        await File.WriteAllTextAsync(ServicePath, serviceContent);

        await RaspberryPiCommandHelper.RunCommandAsync("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        await RaspberryPiCommandHelper.RunCommandAsync("systemctl", $"enable {ServiceName}", "Cannot enable service");

        logger.LogInformation("Autostart configured successfully for executable file: `{Path} {Args}`", GetRealAppPath(), appArguments);

    }
}

