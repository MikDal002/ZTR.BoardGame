using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

public interface ISystemConfigurer
{
    bool IsConfigurationNeeded();
    void Configure();
}

class ConfigureRaspberryPi(ILogger<ConfigureRaspberryPi> logger) : ISystemConfigurer
{
    public bool IsConfigurationNeeded()
    {
        if (!IsRaspberryPi())
        {
            return false;
        }

        var i2cEnabled = IsI2CEnabled();
        var avahiInstalled = IsAvahiInstalled();

        logger.LogInformation("System check -> I2C Enabled: {I2C}, Avahi Installed: {Avahi}", i2cEnabled, avahiInstalled);

        return !i2cEnabled || !avahiInstalled;
    }

    private bool IsI2CEnabled()
    {
        try
        {
            var output = RunCommand("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            logger.LogInformation("Raspberry Pi I2C status: {Output}", output == "0" ? "Enabled" : "Disabled");
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private bool IsAvahiInstalled()
    {
        try
        {
            RunCommand("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true, redirectStandardError: true);
            logger.LogInformation("Avahi Daemon Status installed");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Configure()
    {
        logger.LogInformation("Starting system configuration...");

        EnableI2C();
        InstallAvahi();

        logger.LogInformation("System configuration finished.");
    }

    private void EnableI2C()
    {
        logger.LogInformation("Enabling I2C");
        RunCommand("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
    }

    private void InstallAvahi()
    {
        logger.LogInformation("Installing avahi-daemon...");
        // -y jest kluczowe, żeby nie pytał "Do you want to continue? [Y/n]"
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
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{errorMessage} Run in terminal `{command} {arguments}` or run this app with sudo for first time.");
        }

        return output.Trim();
    }
}
