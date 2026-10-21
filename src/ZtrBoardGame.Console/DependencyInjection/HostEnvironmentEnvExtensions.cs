using Microsoft.Extensions.Hosting;

namespace ZtrBoardGame.Console.DependencyInjection;

public static class HostEnvironmentEnvExtensions
{
    public static bool IsE2ETest(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsEnvironment("E2E");
    }
}
