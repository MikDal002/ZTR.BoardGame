using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public class BoardRunSettings : CommandSettings
{
    [CommandOption("--no-server")]
    public bool NoServer { get; set; }
}

public class BoardRunCommand(TypeRegistrar typeRegistrar) : CancellableAsyncCommand<BoardRunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, BoardRunSettings runSettings, CancellationToken cancellationToken)
    {
        try
        {
            return await RunWebServer(context, runSettings.NoServer, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignore the task canceled exception
        }

        return 0;
    }

    async Task<int> RunWebServer(CommandContext context, bool runInOfflineMode, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());

        var configurationStrategy = BuildConfigurationStrategy.GetStrategy(runInOfflineMode);

        configurationStrategy.ConfigureServices(builder);

        AddBoardServices(builder);

        var app = builder.Build();
        configurationStrategy.ConfigureApp(app);

        await app.RunAsync(cancellationToken);
        return 0;
    }

    void AddBoardServices(WebApplicationBuilder builder)
    {
        foreach (var copyOfService in typeRegistrar.GetCopyOfServices())
        {
            builder.Services.Add(copyOfService);
        }

        if (builder.Environment.IsE2ETest())
        {
            builder.Services.AddSingleton<IGameStrategy, MockedGameStrategy>();
        }

        builder.Services.AddSingleton<IHostedService, BoardGameService>();
        builder.Services.AddSingleton<IBoardGameStatusStorage, BoardGameStatusStorage>();
    }
}

