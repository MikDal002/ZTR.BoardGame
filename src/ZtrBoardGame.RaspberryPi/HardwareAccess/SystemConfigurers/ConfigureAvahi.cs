namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public class ConfigureAvahi() : ISystemConfigurer
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

    private static void InstallAvahi()
        => RaspberryPiCommandHelper.RunCommand("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
}
