using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public class ConfigureAvahi(ILogger<ConfigureAvahi> logger) : ISystemConfigurer
{

    public bool IsConfigurationNeeded()
        => !IsAvahiInstalled();

    public bool CanConfigure()
        => RaspberryPiCommandHelper.IsRaspberryPi();

    public void Configure()
        => InstallAvahi();

    public string Name => "Avahi";

    private static bool IsAvahiInstalled()
    {
        try
        {
            RaspberryPiCommandHelper.RunCommand("dpkg", "-s avahi-daemon", "Cannot check if Avahi is installed", redirectStandardOutput: true, redirectStandardError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void InstallAvahi()
    {
        logger.LogInformation("Installing avahi-daemon...");
        RaspberryPiCommandHelper.RunCommand("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }
}
