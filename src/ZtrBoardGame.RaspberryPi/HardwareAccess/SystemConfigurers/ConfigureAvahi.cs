using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
public class ConfigureAvahi() : ISystemConfigurer
{
    public bool IsConfigurationNeeded()
        => !IsAvahiInstalled();

    public bool CanConfigure()
        => RaspberryPiCommandHelper.IsRaspberryPiRoot();

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
    {
        RaspberryPiCommandHelper.RunUpdate();
        RaspberryPiCommandHelper.RunCommand("apt-get", "install -y avahi-daemon", "Cannot install avahi-daemon");
    }
}
