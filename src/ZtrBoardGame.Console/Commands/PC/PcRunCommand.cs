using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.DependencyInjection;

namespace ZtrBoardGame.Console.Commands.PC;

public class PcRunSettings : CommandSettings
{
    [CommandOption("--non-interactive")]
    public bool NonInteractive { get; set; }
}

public class PcRunCommand(TypeRegistrar typeRegistrar, IAnsiConsole console, IBoardStorage boardStorage, IGameService gameService) : CancellableAsyncCommand<PcRunSettings>
{

    public override async Task<int> ExecuteAsync(CommandContext context, PcRunSettings runSettings, CancellationToken cancellationToken)
    {
        var webTask = RunWebServer(context, cancellationToken);

        if (runSettings.NonInteractive)
        {
            await NonInteractiveStrategy(cancellationToken);
        }
        else
        {
            await StandardStrategy(cancellationToken);
        }

        return await webTask;
    }

    async Task StandardStrategy(CancellationToken cancellationToken)
    {
        bool confirmAsync;
        do
        {
            confirmAsync = await console.ConfirmAsync("Wciśnij enter aby zacząć grę");
            if (!confirmAsync)
            {
                continue;
            }

            var boards = boardStorage.GetAllAddresses().ToList();
            if (boards.Count == 0)
            {
                console.MarkupLine("[red]Brak dostępnych plansz![/]");
                continue;
            }

            console.MarkupLine($"[green]Rozpoczynanie gry dla [/] {boardStorage.Count} graczy");
            await gameService.StartSessionAsync(cancellationToken);
        } while (confirmAsync);
    }

    async Task NonInteractiveStrategy(CancellationToken cancellationToken)
    {
        console.MarkupLine("[yellow]Non-interactive mode. Waiting for boards to connect...[/]");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!boardStorage.GetAllAddresses().Any() && !cancellationToken.IsCancellationRequested)
        {
            if (stopwatch.Elapsed > System.TimeSpan.FromSeconds(30))
            {
                console.MarkupLine("[red]Error: Timed out waiting for boards to connect.[/]");
                return;
            }

            await Task.Delay(1000, cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        console.MarkupLine($"[green]Starting game for [/] {boardStorage.Count} players.");
        await gameService.StartSessionAsync(cancellationToken);
    }

    private async Task<int> RunWebServer(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());
        foreach (var copyOfService in typeRegistrar.GetCopyOfServices())
        {
            if (copyOfService.ServiceType == typeof(IBoardStorage))
            {
                builder.Services.AddSingleton(boardStorage);
            }
            else if (copyOfService.ServiceType == typeof(IGameService))
            {
                builder.Services.AddSingleton(gameService);
            }
            else
            {
                builder.Services.Add(copyOfService);
            }
        }

        builder.Services.AddSingleton<IHostedService, BoardConnectionCheckerService>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        var app = builder.Build();

        app.UseForwardedHeaders();
        app.MapControllers();
        await app.RunAsync(cancellationToken);
        return 0;
    }
}
