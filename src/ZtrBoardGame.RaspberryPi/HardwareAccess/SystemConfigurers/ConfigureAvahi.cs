using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
class ConfigureAvahi(ILogger<ConfigureAvahi> logger) : ISystemConfigurer
{
    public async Task<bool> IsConfigurationNeededAsync()
    {
        var isAvahiInstalled = await IsAvahiInstalledAsync();
        logger.LogInformation(
            "System check -> Avahi Installed: {Avahi}", isAvahiInstalled);
        return !isAvahiInstalled;
    }

    public Task<bool> CanConfigureAsync()
        => RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task ConfigureAsync()
        => await InstallAvahiAsync();

    public string Name => "Avahi";

    private static async Task<bool> IsAvahiInstalledAsync()
    {
        try
        {
            await RaspberryPiCommandHelper.RunCommandAsync("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task InstallAvahiAsync()
    {
        await RaspberryPiCommandHelper.RunUpdate();
        await RaspberryPiCommandHelper.RunCommandAsync("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }
}
