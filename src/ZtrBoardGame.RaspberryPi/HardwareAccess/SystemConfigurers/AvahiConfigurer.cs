using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

class AvahiConfigurer(ILogger<AvahiConfigurer> logger) : ISystemConfigurer
{
    public bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public async Task<bool> IsConfigurationNeededAsync()
    {
        var isAvahiInstalled = await IsAvahiInstalledAsync();
        logger.LogInformation(
            "System check -> Avahi Installed: {Avahi}", isAvahiInstalled);
        return !isAvahiInstalled;
    }

    public async Task ConfigureAsync()
        => await InstallAvahiAsync();

    public string Name { get; } = "Avahi";

    private async Task InstallAvahiAsync()
    {
        logger.LogInformation("Installing avahi-daemon...");
        await RaspberryPiSystemInfo.RunCommandAsync("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }

    private static async Task<bool> IsAvahiInstalledAsync()
    {
        try
        {
            await RaspberryPiSystemInfo.RunCommandAsync("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true, redirectStandardError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
