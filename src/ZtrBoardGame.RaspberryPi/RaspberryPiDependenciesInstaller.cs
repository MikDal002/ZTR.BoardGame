using Microsoft.Extensions.DependencyInjection;
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.RaspberryPi;

public static class RaspberryPiDependenciesInstaller
{
    public static IServiceCollection AddRaspberryPiGameStrategy(this IServiceCollection services)
    {
        services.AddSingleton<IGameStrategy, OnRaspberryPiGameStrategy>();
        services.AddSingleton<IModule, FourFieldKubasModule>();
        return services;
    }

    public static IServiceCollection AddRaspberryPiHardwareConfigurer(this IServiceCollection services)
    {
        services.AddSingleton<ISystemConfigurer, ConfigureRaspberryPi>();
        return services;
    }
}
