using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
class ConfigureI2C() : ISystemConfigurer
{
    public string Name => "I2C Bus";

    public bool CanConfigure()
        => RaspberryPiCommandHelper.IsRaspberryPiRoot();

    public bool IsConfigurationNeeded()
        => !IsI2CEnabled();

    public void Configure()
        => EnableI2C();

    private static bool IsI2CEnabled()
    {
        try
        {
            var output = RaspberryPiCommandHelper.RunCommand("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private static void EnableI2C()
        => RaspberryPiCommandHelper.RunCommand("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
}
