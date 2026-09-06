
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.PC.UI;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.Server.Commons;

namespace ZtrBoardGame.Console.Commands.PC;

public class PcRunSettings : CommandSettings
{
    [CommandOption("--non-interactive")]
    public bool NonInteractive { get; set; }

    [CommandOption("--new-ui")]
    public bool NewUi { get; set; }
}

public class PcRunCommand(TypeRegistrar typeRegistrar, IAnsiConsole console, IBoardStorage boardStorage, IGameService gameService, ILiveGameDashboard liveGameDashboard, IAvaloniaGameDashboard avaloniaGameDashboard) : AsyncCommand<PcRunSettings>
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

        try
        {
            if (runSettings.NewUi)
            {
                await avaloniaGameDashboard.StartUI(cancellationToken);
            }
            else
            {
                await NewPrompting(cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Łapiemy wyjątek rzucany przez tcs.TrySetCanceled(),
            // żeby serwer w tle mógł się czysto zamknąć
        }
    }

    async Task NewPrompting(CancellationToken cancellationToken)
    {
        await liveGameDashboard.NewPrompting(cancellationToken);
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

        ShowLeaderBoard(gameService.Results);
    }

    private static void ShowLeaderBoard(IReadOnlyDictionary<Server.Commons.Board, GameResult> results)
    {
        var sorted = results.OrderBy(kv => kv.Value.Duration).ToList();

        var table = new Table()
            .RoundedBorder()
            .AddColumn("Miejsce")
            .AddColumn("Board")
            .AddColumn("Czas (ms)");

        var place = 1;
        foreach (var kv in sorted)
        {
            var board = kv.Key;
            var result = kv.Value;
            table.AddRow(place.ToString(), board.Address.ToString(), result.Duration.TotalMilliseconds.ToString());
            place++;
        }

        AnsiConsole.Write(new FigletText("Wyniki"));
        AnsiConsole.Write(table);
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
