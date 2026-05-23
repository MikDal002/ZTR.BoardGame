using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

#pragma warning disable S101 // This name is appropriate for physical interface I2C.
class I2CConfigurer(ILogger<I2CConfigurer> logger) : ISystemConfigurer
#pragma warning restore S101
{
    public bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public async Task<bool> IsConfigurationNeededAsync()
    {
        var i2cEnabled = await IsI2CEnabledAsync();
        logger.LogInformation("System check -> I2C Enabled: {I2C}", i2cEnabled);
        return !i2cEnabled;
    }

    public async Task ConfigureAsync()
        => await EnableI2CAsync();

    public string Name { get; } = "I2C";

    private static async Task<bool> IsI2CEnabledAsync()
    {
        try
        {
            var output = await RaspberryPiSystemInfo.RunCommandAsync("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private async Task EnableI2CAsync()
    {
        logger.LogInformation("Enabling I2C");
        await RaspberryPiSystemInfo.RunCommandAsync("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
    }
}
