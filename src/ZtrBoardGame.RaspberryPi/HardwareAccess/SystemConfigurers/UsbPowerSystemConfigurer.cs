using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

class UsbPowerSystemConfigurer(ILogger<UsbPowerSystemConfigurer> logger) : ISystemConfigurer
{
    private const string CronEntry = "@reboot uhubctl -a off";

    public string Name => "USB Power Management (uhubctl)";

    public bool CanConfigure()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public bool IsConfigurationNeeded()
    {
        if (!RaspberryPiSystemInfo.IsRaspberryPi())
        {
            return false;
        }

        var uhubctlInstalled = IsUhubctlInstalled();
        var cronConfigured = IsCronConfigured();

        logger.LogInformation("USB Power Check -> uhubctl Installed: {Installed}, Cron Configured: {Cron}",
            uhubctlInstalled, cronConfigured);

        return !uhubctlInstalled || !cronConfigured;
    }

    public void Configure()
    {
        logger.LogInformation("Starting USB power configuration...");

        if (!IsUhubctlInstalled())
        {
            InstallUhubctl();
        }

        if (!IsCronConfigured())
        {
            ConfigureCron();
        }

        logger.LogInformation("USB power configuration finished.");
    }

    private static bool IsUhubctlInstalled()
    {
        try
        {
            RaspberryPiSystemInfo.RunCommand("dpkg", "-s uhubctl", "Check uhubctl status", redirectStandardOutput: true, redirectStandardError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCronConfigured()
    {
        try
        {
            // crontab -l returns 1 if no crontab for user, which throws exception in RunCommand
            var output = RaspberryPiSystemInfo.RunCommand("crontab", "-l", "Read crontab", redirectStandardOutput: true, redirectStandardError: true);
            return output.Contains(CronEntry);
        }
        catch
        {
            return false;
        }
    }

    private void InstallUhubctl()
    {
        logger.LogInformation("Installing uhubctl...");
        RaspberryPiSystemInfo.RunCommand("apt-get", "install -y uhubctl", "Cannot install uhubctl");
    }

    private void ConfigureCron()
    {
        logger.LogInformation("Adding USB power off to crontab...");
        try
        {
            // Get current crontab, append new entry, and pipe back to crontab
            // If crontab is empty, we just set the entry
            var currentCrontab = string.Empty;
            try
            {
                currentCrontab = RaspberryPiSystemInfo.RunCommand("crontab", "-l", "Read crontab", redirectStandardOutput: true, redirectStandardError: true);
            }
            catch
            {
                // Ignore error if crontab doesn't exist yet
            }

            var newCrontab = string.IsNullOrEmpty(currentCrontab)
                ? CronEntry
                : $"{currentCrontab}\n{CronEntry}";

            // Using temporary file to avoid complex piping in RunCommand
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, newCrontab + "\n");

            try
            {
                RaspberryPiSystemInfo.RunCommand("crontab", tempFile, "Update crontab");
            }
            finally
            {
                File.Delete(tempFile);
            }

            logger.LogInformation("Crontab updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update crontab.");
            throw;
        }
    }
}
