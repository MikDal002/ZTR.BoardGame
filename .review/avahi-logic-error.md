Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/ConfigureAvahi.cs`, `IsConfigurationNeededAsync` method

What is wrong and why:
There is a logic error identical to the one in `I2CConfigurer`. The method `IsConfigurationNeededAsync` returns `isAvahiInstalled`. This means if Avahi is already installed, it will tell the system it *needs* to be configured (triggering a reinstall), but if it is NOT installed, it will skip configuration entirely. This defeats the entire purpose of the setup step.

Proposed solution:
Return `!isAvahiInstalled;` to ensure the installation is only triggered when Avahi is missing from the system.
