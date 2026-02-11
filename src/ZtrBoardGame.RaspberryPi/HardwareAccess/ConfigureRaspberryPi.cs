using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

public interface ISystemConfigurer
{
    bool IsConfigurationNeeded();
    void Configure();
}

class ConfigureRaspberryPi(ILogger<ConfigureRaspberryPi> logger) : ISystemConfigurer
{
    private const string ServiceName = "ztrboardgame.service";
    private const string ServicePath = $"/etc/systemd/system/{ServiceName}";
    private const string RealAppPath = "/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage";

    public bool IsConfigurationNeeded()
    {
        if (!IsRaspberryPi())
        {
            return false;
        }

        var i2cEnabled = IsI2CEnabled();
        var avahiInstalled = IsAvahiInstalled();
        var serviceConfigured = IsServiceConfigured();

        logger.LogInformation(
            "System check -> I2C Enabled: {I2C}, Avahi Installed: {Avahi}, AutoStart Configured: {Service}",
            i2cEnabled, avahiInstalled, serviceConfigured);

        return !i2cEnabled || !avahiInstalled || !serviceConfigured;
    }

    public void Configure()
    {
        logger.LogInformation("Starting system configuration...");

        if (!IsI2CEnabled())
        {
            EnableI2C();
        }

        if (!IsAvahiInstalled())
        {
            InstallAvahi();
        }

        if (!IsServiceConfigured())
        {
            InstallSystemdService();
        }

        logger.LogInformation("System configuration finished.");
    }

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
            var status = RunCommand("systemctl", $"is-enabled {ServiceName}", "Check service status", redirectStandardOutput: true);
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
        logger.LogInformation("Service file created: {Path} {Args}", RealAppPath, appArguments);

        RunCommand("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        RunCommand("systemctl", $"enable {ServiceName}", "Cannot enable service");

        logger.LogInformation("Autostart configured successfully.");

    }

    private static bool IsI2CEnabled()
    {
        try
        {
            var output = RunCommand("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAvahiInstalled()
    {
        try
        {
            RunCommand("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true, redirectStandardError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EnableI2C()
    {
        logger.LogInformation("Enabling I2C");
        RunCommand("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
    }

    private void InstallAvahi()
    {
        logger.LogInformation("Installing avahi-daemon...");
        RunCommand("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }

    private static bool IsRaspberryPi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return false;
        }

        try
        {
            const string modelPath = "/proc/device-tree/model";
            if (File.Exists(modelPath))
            {
                var model = File.ReadAllText(modelPath);
                return model.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string RunCommand(string command, string arguments, string errorMessage,
        bool redirectStandardOutput = false, bool redirectStandardError = false)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError
        };

        using var process = Process.Start(processStartInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Cannot start new process because of unknown error");
        }

        var output = string.Empty;

        if (redirectStandardOutput)
        {
            output = process.StandardOutput.ReadToEnd();
        }

        process.WaitForExit();

        // Jeśli proces zwróci błąd, rzucamy wyjątek (który jest łapany w metodach Is...)
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{errorMessage} (Exit Code: {process.ExitCode})");
        }

        return output.Trim();
    }
}
