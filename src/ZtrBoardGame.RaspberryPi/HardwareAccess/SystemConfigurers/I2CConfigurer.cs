using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

#pragma warning disable S101 // This name is appropriate for physical interface I2C.
class I2CConfigurer(ILogger<I2CConfigurer> logger) : ISystemConfigurer
#pragma warning restore S101
{
    public bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public bool IsConfigurationNeeded()
    {
        var i2cEnabled = IsI2CEnabled();
        logger.LogInformation("System check -> I2C Enabled: {I2C}", i2cEnabled);
        return i2cEnabled;
    }

    public void Configure()
        => EnableI2C();

    public string Name { get; } = "I2C";

    private static bool IsI2CEnabled()
    {
        try
        {
            var output = RaspberryPiSystemInfo.RunCommand("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private void EnableI2C()
    {
        logger.LogInformation("Enabling I2C");
        RaspberryPiSystemInfo.RunCommand("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
    }
}
