using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.PC.UI;
using ZtrBoardGame.Console.DependencyInjection;

namespace ZtrBoardGame.Console.Commands.PC;

public class PcRunSettings : CommandSettings
{
    [CommandOption("--non-interactive")]
    public bool NonInteractive { get; set; }

    [CommandOption("--new-ui")]
    public bool NewUi { get; set; }
}

public class PcRunCommand(TypeRegistrar typeRegistrar, IAnsiConsole console, IBoardStorage boardStorage, IGameService gameService, ILiveGameDashboard liveGameDashboard) : AsyncCommand<PcRunSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, PcRunSettings runSettings, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var webTask = RunWebServer(context, cts.Token);

        var strategyTask = runSettings.NonInteractive
            ? NonInteractiveStrategy(cts.Token)
            : StandardStrategy(cts.Token, runSettings);

        var completedTask = await Task.WhenAny(webTask, strategyTask);

        await cts.CancelAsync();

        if (completedTask == webTask)
        {
            return await webTask;
        }

        try
        {
            await strategyTask;
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }

        return 0;
    }

    async Task StandardStrategy(CancellationToken cancellationToken, PcRunSettings runSettings)
    {
        if (runSettings.NewUi)
        {
            await NewPrompting(cancellationToken);
        }
        else
        {
            await OldPrompting(cancellationToken);
        }
    }

    async Task NewPrompting(CancellationToken cancellationToken)
    {
        await liveGameDashboard.NewPrompting(cancellationToken);
    }

    async Task OldPrompting(CancellationToken cancellationToken)
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
            do
            {
                AnsiConsole.MarkupLine($"[grey]Oczekiwanie na wyniki od graczy[/]");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            } while (!gameService.AreAllResultsReceived());

            gameService.ShowLeaderBoard();

        } while (!cancellationToken.IsCancellationRequested);
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
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        } while (!gameService.AreAllResultsReceived() && !cancellationToken.IsCancellationRequested);

        gameService.ShowLeaderBoard();
    }

    private async Task<int> RunWebServer(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());
        builder.Logging.ClearProviders();

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
