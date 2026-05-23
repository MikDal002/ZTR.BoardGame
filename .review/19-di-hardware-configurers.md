# Location and context
src/ZtrBoardGame.RaspberryPi/RaspberryPiDependenciesInstaller.cs:22

```csharp
    public static IServiceCollection AddRaspberryPiHardwareConfigurers(this IServiceCollection services)
    {
        services.AddSingleton<ISystemConfigurer, SystemdConfigurer>();
        services.AddSingleton<ISystemConfigurer, AvahiConfigurer>();
        services.AddSingleton<ISystemConfigurer, I2CConfigurer>();
        services.AddSingleton<ISystemConfigurer, BootConfigSystemConfigurer>();
        services.AddSingleton<ISystemConfigurer, UsbPowerSystemConfigurer>();
        return services;
    }
```

Code is used in `RaspberryPiDependenciesInstaller` to register system configurers in the DI container.

# What is wrong
All `ISystemConfigurer` implementations are registered as singletons. While this might be perfectly fine if they have no state, some configurers (like `SystemdConfigurer` which takes an `ILogger`) might theoretically capture state or be intended for transient use. Given that configuration setup is generally a one-time operation at startup or explicitly via a command, `Transient` or `Scoped` is often a safer default for command-line tool dependencies to ensure clean state per execution, especially in tools like Spectre.Console where commands might be instantiated per run. Though in this specific project setup, Singletons likely work, it's worth questioning.

# Solution
Consider if `Transient` is more appropriate. If they are truly stateless, `Singleton` is fine, but `Transient` is the safer default for command dependencies unless shared state is required.

```csharp
    public static IServiceCollection AddRaspberryPiHardwareConfigurers(this IServiceCollection services)
    {
        services.AddTransient<ISystemConfigurer, SystemdConfigurer>();
        // ...
```

# Assessment
1/6 - It is highly likely Singleton is perfectly fine here and this is more of a pedantic observation than a bug.
