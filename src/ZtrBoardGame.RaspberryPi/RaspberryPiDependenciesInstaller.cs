using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.RaspberryPi.HardwareAccess;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.RaspberryPi;

public static class RaspberryPiDependenciesInstaller
{
    public static IServiceCollection AddRaspberryPiGameStrategy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PhysicalBoardSettings>(configuration.GetSection(nameof(PhysicalBoardSettings)));

        services.AddScoped<IGameStrategy, OnRaspberryPiGameStrategy>();
        services.AddScoped<IPhysicalBoard, I2CPhysicalBoard>();
        services.AddScoped<IPhysicalNotificator, I2CPhysicalBoard>();
        return services;
    }

    public static IServiceCollection AddRaspberryPiHardwareConfigurers(this IServiceCollection services)
    {
        services.AddTransient<ISystemConfigurer, ConfigureSystemd>();
        services.AddTransient<ISystemConfigurer, ConfigureAvahi>();
        services.AddTransient<ISystemConfigurer, ConfigureI2C>();
        services.AddTransient<ISystemConfigurer, BootConfigSystemConfigurer>();
        services.AddTransient<ISystemConfigurer, ConfigureUsbPowerSystem>();
        return services;
    }
}
