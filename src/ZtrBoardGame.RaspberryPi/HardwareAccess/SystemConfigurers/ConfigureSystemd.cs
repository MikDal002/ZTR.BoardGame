using Microsoft.Extensions.Logging;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

class ConfigureSystemd(ILogger<ConfigureSystemd> logger) : ISystemConfigurer
{
    private const string ServiceName = "ztrboardgame.service";
    private const string ServicePath = $"/etc/systemd/system/{ServiceName}";
    private const string RealAppPath = "/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage";

    public bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public bool IsConfigurationNeeded()
    {
        var serviceConfigured = IsServiceConfigured();
        logger.LogInformation("System check -> AutoStart Configured: {Service}", serviceConfigured);
        return !serviceConfigured;
    }

    public void Configure()
        => InstallSystemdService();

    public string Name { get; } = "Systemd";

    private bool IsServiceConfigured()
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
            var serviceContent = File.ReadAllText(ServicePath);
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
            var status = RaspberryPiSystemInfo.RunCommand("systemctl", $"is-enabled {ServiceName}", "Check service status", redirectStandardOutput: true);
            return status.Trim() == "enabled";
        }
        catch
        {
            return false;
        }
    }

    private void InstallSystemdService()
    {
        logger.LogInformation("Installing systemd service for auto-start...");

        var autoConfigOptionName = CommandOptionExtensions.GetLongOptionName<GlobalCommandSettings, bool>(x => x.AutomaticallyConfigureHardware);
        var appArguments = $"board run {autoConfigOptionName}";

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

        File.WriteAllText(ServicePath, serviceContent);

        RaspberryPiSystemInfo.RunCommand("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        RaspberryPiSystemInfo.RunCommand("systemctl", $"enable {ServiceName}", "Cannot enable service");

        logger.LogInformation("Autostart configured successfully for executable file: `{Path} {Args}`", RealAppPath, appArguments);

    }
}

