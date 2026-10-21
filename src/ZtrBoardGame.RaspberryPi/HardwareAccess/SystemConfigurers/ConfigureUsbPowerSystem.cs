using Microsoft.Extensions.Logging;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

class ConfigureUsbPowerSystem(ILogger<ConfigureUsbPowerSystem> logger) : ISystemConfigurer
{
    private const string CronEntry = "@reboot sudo uhubctl -l 1 -a off && sudo uhubctl -l 3 -a off";

    public string Name => "USB Power Management (uhubctl)";

    public Task<bool> CanConfigureAsync()
        => RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task<bool> IsConfigurationNeededAsync()
    {
        var uhubctlInstalled = await IsUhubctlInstalledAsync();
        var cronConfigured = await IsCronConfiguredAsync();

        logger.LogInformation("USB Power Check -> uhubctl Installed: {Installed}, Cron Configured: {Cron}",
            uhubctlInstalled, cronConfigured);

        return !uhubctlInstalled || !cronConfigured;
    }

    public async Task ConfigureAsync()
    {
        logger.LogInformation("Starting USB power configuration...");

        if (!await IsUhubctlInstalledAsync())
        {
            await InstallUhubctlAsync();
        }

        if (!await IsCronConfiguredAsync())
        {
            await ConfigureCronAsync();
        }

        logger.LogInformation("USB power configuration finished.");
    }

    private static async Task<bool> IsUhubctlInstalledAsync()
    {
        try
        {
            await RaspberryPiCommandHelper.RunCommandAsync("dpkg", "-s uhubctl", "Check uhubctl status");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsCronConfiguredAsync()
    {
        try
        {
            // crontab -l returns 1 if no crontab for user, which throws exception in RunCommandAsync
            var output = await RaspberryPiCommandHelper.RunCommandAsync("crontab", "-l", "Read crontab");
            return output.Contains(CronEntry);
        }
        catch
        {
            return false;
        }
    }

    private async Task InstallUhubctlAsync()
    {
        logger.LogInformation("Installing uhubctl...");
        await RaspberryPiCommandHelper.RunCommandAsync("apt-get", "install -y uhubctl", "Cannot install uhubctl");
    }

    private async Task ConfigureCronAsync()
    {
        logger.LogInformation("Adding USB power off to crontab...");
        try
        {
            // Get current crontab, append new entry, and pipe back to crontab
            // If crontab is empty, we just set the entry
            var currentCrontab = string.Empty;
            try
            {
                currentCrontab = await RaspberryPiCommandHelper.RunCommandAsync("crontab", "-l", "Read crontab");
            }
            catch
            {
                // Ignore error if crontab doesn't exist yet
            }

            var newCrontab = string.IsNullOrEmpty(currentCrontab)
                ? CronEntry
                : $"{currentCrontab}\n{CronEntry}";

            // Using temporary file to avoid complex piping in RunCommandAsync
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            await File.WriteAllTextAsync(tempFile, newCrontab + "\n");

            try
            {
                await RaspberryPiCommandHelper.RunCommandAsync("crontab", tempFile, "Update crontab");
            }
            finally
            {
                File.Delete(tempFile);
            }

            logger.LogInformation("Crontab updated successfully.");
        }
        catch (Exception ex)
        {
            throw new SystemConfigurationException("Failed to update crontab.", ex);
        }
    }
}
