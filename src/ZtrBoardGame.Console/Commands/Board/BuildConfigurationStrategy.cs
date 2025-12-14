using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZtrBoardGame.Console.Commands.Board.Offline;
using ZtrBoardGame.Console.Commands.Board.Online;

namespace ZtrBoardGame.Console.Commands.Board;

class BuildConfigurationStrategy
{
    public static IConfigurationStrategy GetStrategy(bool runInOfflineMode)
    {
        if (runInOfflineMode)
        {
            return new OfflineConfigurationStrategy();
        }
        else
        {
            return new OnlineConfigurationStrategy();
        }
    }
}
interface IConfigurationStrategy
{
    void ConfigureServices(WebApplicationBuilder builder);
    void ConfigureApp(WebApplication app);
}

class OfflineConfigurationStrategy : IConfigurationStrategy
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IGameStarter, OfflineGameStarter>();
        builder.Services.AddScoped<IResultSender, OfflineResultSender>();
    }
    public void ConfigureApp(WebApplication app)
    {
        // No specific app configuration needed for offline mode
    }
}

class OnlineConfigurationStrategy : IConfigurationStrategy
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHostedService, HelloService>();
        builder.Services.AddScoped<IResultSender, OnlineResultSender>();
        builder.Services.AddSingleton<IGameStarter, OnlineGameStarter>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
    }
    public void ConfigureApp(WebApplication app)
    {
        app.UseForwardedHeaders();
        app.MapControllers();
    }
}
