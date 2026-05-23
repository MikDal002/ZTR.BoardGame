Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/I2CConfigurer.cs`, `IsConfigurationNeededAsync` method

What is wrong and why:
There is a logic error in determining if the configuration is needed. The method returns `i2cEnabled` directly. This means if I2C is *already* enabled (`i2cEnabled` is true), the application concludes that configuration *is needed* and proceeds to configure it again. If I2C is disabled (`i2cEnabled` is false), it concludes configuration is *not* needed, completely ignoring it. This is exactly the opposite of the intended behavior.

Proposed solution:
Return `!i2cEnabled;` in `IsConfigurationNeededAsync`. This ensures that configuration is only requested when the feature is currently disabled.
