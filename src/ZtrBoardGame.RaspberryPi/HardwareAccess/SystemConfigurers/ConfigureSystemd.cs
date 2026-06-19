using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
class ConfigureSystemd(ILogger<ConfigureSystemd> logger) : ISystemConfigurer
{
    public string Name => "Systemd Autostart Service";

    private const string ServiceName = "ztrboardgame.service";
    private const string ServicePath = $"/etc/systemd/system/{ServiceName}";
    private const string RealAppPath = "/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage";

    public Task<bool> CanConfigureAsync()
        => RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task<bool> IsConfigurationNeededAsync()
        => !(await IsServiceConfigured());

    public async Task ConfigureAsync()
        => await InstallSystemdService();

    private async Task<bool> IsServiceConfigured()
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

        try
        {
            var status = await RaspberryPiCommandHelper.RunCommandAsync("systemctl", $"is-enabled {ServiceName}", "Check service status", redirectStandardOutput: true);
            return status.Trim() == "enabled";
        }
        catch
        {
            return false;
        }
    }

    private async Task InstallSystemdService()
    {
        logger.LogInformation("Installing systemd service for auto-start...");

        var appArguments = "board run";

        if (!File.Exists(RealAppPath))
        {
            throw new FileNotFoundException($"Executable file not found: {RealAppPath}");
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
        logger.LogInformation("Service file created: {Path} {Args}", RealAppPath, appArguments);

        await RaspberryPiCommandHelper.RunCommandAsync("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        await RaspberryPiCommandHelper.RunCommandAsync("systemctl", $"enable {ServiceName}", "Cannot enable service");

        logger.LogInformation("Autostart configured successfully.");

    }
}
