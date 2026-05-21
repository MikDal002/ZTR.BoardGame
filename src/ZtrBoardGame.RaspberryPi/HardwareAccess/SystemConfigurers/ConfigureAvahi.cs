using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

class ConfigureAvahi(ILogger<ConfigureAvahi> logger) : ISystemConfigurer
{
    public bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public bool IsConfigurationNeeded()
    {
        var isAvahiInstalled = IsAvahiInstalled();
        logger.LogInformation(
            "System check -> Avahi Installed: {Avahi}", isAvahiInstalled);
        return isAvahiInstalled;
    }

    public void Configure()
        => InstallAvahi();

    public string Name { get; } = "Avahi";

    private void InstallAvahi()
    {
        logger.LogInformation("Installing avahi-daemon...");
        RaspberryPiSystemInfo.RunCommand("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }

    private static bool IsAvahiInstalled()
    {
        try
        {
            RaspberryPiSystemInfo.RunCommand("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true, redirectStandardError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
