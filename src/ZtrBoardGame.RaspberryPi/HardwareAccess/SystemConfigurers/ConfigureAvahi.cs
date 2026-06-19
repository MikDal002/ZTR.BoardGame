using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
public class ConfigureAvahi() : ISystemConfigurer
{
    public async Task<bool> IsConfigurationNeededAsync()
        => !await IsAvahiInstalledAsync();

    public Task<bool> CanConfigureAsync()
        => RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task ConfigureAsync()
        => await InstallAvahi();

    public string Name => "Avahi";

    private static async Task<bool> IsAvahiInstalledAsync()
    {
        try
        {
            await RaspberryPiCommandHelper.RunCommandAsync("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task InstallAvahi()
    {
        await RaspberryPiCommandHelper.RunUpdate();
        await RaspberryPiCommandHelper.RunCommandAsync("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }
}
