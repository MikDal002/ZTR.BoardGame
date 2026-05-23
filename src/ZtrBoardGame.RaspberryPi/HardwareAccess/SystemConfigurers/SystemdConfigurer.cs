using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

class SystemdConfigurer(ILogger<SystemdConfigurer> logger) : ISystemConfigurer
{
    private const string ServiceName = "ztrboardgame.service";
    private const string ServicePath = $"/etc/systemd/system/{ServiceName}";

    /// <summary>
    /// Unfortunately Environment.ProcessPath and AppContext.BaseDirectory returns path to the /tmp/.mound.../
    /// directory, which is changed on every run and is not the real path to the executable file. This is because of the
    /// way AppImage works.
    /// </summary>
    private const string RealAppPath = "/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage";

    public bool CanConfigure()
    {
        return RaspberryPiSystemInfo.IsRaspberryPi();
    }

    public async Task<bool> IsConfigurationNeededAsync()
    {
        var serviceConfigured = await IsServiceConfiguredAsync();
        logger.LogInformation("System check -> AutoStart Configured: {Service}", serviceConfigured);
        return !serviceConfigured;
    }

    public async Task ConfigureAsync()
        => await InstallSystemdServiceAsync();

    public string Name { get; } = "Systemd";

    private async Task<bool> IsServiceConfiguredAsync()
    {
        if (!File.Exists(ServicePath))
        {
            return false;
        }

        var currentExePath = RealAppPath;
        if (string.IsNullOrEmpty(currentExePath))
        {
            return false;
        }

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

        var status = await RaspberryPiSystemInfo.RunCommandAsync("systemctl", $"is-enabled {ServiceName}", "Check service status", redirectStandardOutput: true);
        return status.Trim() == "enabled";

    }

    private async Task InstallSystemdServiceAsync()
    {
        logger.LogInformation("Installing systemd service for auto-start...");

        var appArguments = $"board run";

        if (!File.Exists(RealAppPath))
        {
            throw new FileNotFoundException($"CRITICAL: Nie znaleziono pliku: {RealAppPath}");
        }

        var workingDirectory = Path.GetDirectoryName(RealAppPath);

        var serviceContent = $@"
[Unit]
Description=Ztr Board Game Service
After=network.target multi-user.target

[Service]
Type=simple
User=root
WorkingDirectory={workingDirectory}
ExecStart={RealAppPath} {appArguments}
Restart=always
RestartSec=5
Environment=DOTNET_ROOT=/usr/share/dotnet
StandardOutput=journal+console
StandardError=journal+console

[Install]
WantedBy=multi-user.target
";

        await File.WriteAllTextAsync(ServicePath, serviceContent);

        await RaspberryPiSystemInfo.RunCommandAsync("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        await RaspberryPiSystemInfo.RunCommandAsync("systemctl", $"enable {ServiceName}", "Cannot enable service");

        logger.LogInformation("Autostart configured successfully for executable file: `{Path} {Args}`", RealAppPath, appArguments);

    }
}

