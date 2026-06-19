using System.Diagnostics.CodeAnalysis;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "Because it is direct hardware access, and there is nothing else to test")]
class ConfigureI2C() : ISystemConfigurer
{
    public string Name => "I2C Bus";

    public async Task<bool> CanConfigureAsync()
        => await RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

    public async Task<bool> IsConfigurationNeededAsync()
        => !await IsI2CEnabledAsync();

    public async Task ConfigureAsync()
        => await EnableI2CAsync();

    private static async Task<bool> IsI2CEnabledAsync()
    {
        try
        {
            var output = await RaspberryPiCommandHelper.RunCommandAsync("raspi-config", "nonint get_i2c", "Cannot check if I2C bus is enabled",
                redirectStandardOutput: true);
            return output == "0";
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnableI2CAsync()
        => await RaspberryPiCommandHelper.RunCommandAsync("raspi-config", "nonint do_i2c 0", "Cannot enable I2C bus", redirectStandardOutput: true);
}
