using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System;
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

    [CommandOption("--new-ui")]
    public bool NewUi { get; set; }
}

public class PcRunCommand(TypeRegistrar typeRegistrar, IAnsiConsole console, IBoardStorage boardStorage, IGameService gameService) : CancellableAsyncCommand<PcRunSettings>
{

    public override async Task<int> ExecuteAsync(CommandContext context, PcRunSettings runSettings, CancellationToken cancellationToken)
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
        await AnsiConsole.Live(new Text("Oczekiwanie na graczy..."))
            .StartAsync(async ctx =>
            {
                do
                {
                    var gameStarted = false;
                    var errorMessage = "";

                    ClearInputBufferBeforeAwaitingPlayerStatus();
                    do
                    {

                        var boards = boardStorage.GetAll().ToList();
                        foreach (var board in boards)
                        {
                            board.Reset();
                        }

                        IRenderable rowsWithBoards;
                        if (!boards.Any())
                        {
                            rowsWithBoards = new Panel("[grey]Oczekiwanie na połączenie przynajmniej jednej planszy...[/]")
                            .Border(BoxBorder.None)
                            .Padding(0, 1);
                        }
                        else
                        {
                            var rows = boards
                                .Select(d => Ui.CreateBoardPanel(d))
                                .ToArray();

                            var splitRows = new Columns(rows);
                            splitRows.Collapse();
                            rowsWithBoards = splitRows;
                        }

                        var promptText = "[yellow]Oczekiwanie na graczy...[/]";
                        if (boards.Any())
                        {
                            promptText += " Wciśnij [green]ENTER[/] lub [green]SPACJĘ[/] aby rozpocząć grę!";
                        }

                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            promptText += $"\n{errorMessage}";
                        }

                        var promptPanel = new Panel(promptText)
                            .Border(BoxBorder.Double)
                            .Expand();

                        ctx.UpdateTarget(new Rows(rowsWithBoards,
                            new Rule(),
                            promptPanel
                        ));

                        while (System.Console.KeyAvailable)
                        {
                            var keyInfo = System.Console.ReadKey(intercept: true);
                            var key = keyInfo.Key;

                            if (key == ConsoleKey.Enter || key == ConsoleKey.Spacebar)
                            {
                                if (boards.Any())
                                {
                                    gameStarted = true;
                                    errorMessage = "";
                                }
                                else
                                {
                                    errorMessage = "[red]Brak plansz! Naciśnij [green]ENTER[/] lub [green]SPACJĘ[/] gdy się pojawią, aby rozpocząć grę.[/]";
                                }
                            }
                            else
                            {
                                errorMessage = "[red]Naciśnięto niewłaściwy klawisz.[/] Wciśnij [green]ENTER[/] lub [green]SPACJĘ[/].";
                            }
                        }

                        await Task.Delay(200, cancellationToken);
                    } while (!gameStarted && !cancellationToken.IsCancellationRequested);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await gameService.StartSessionAsync(cancellationToken);
                    var task = gameService.AwaitResultsFromBoards(cancellationToken);

                    do
                    {
                        var boards = boardStorage.GetAll().ToList();

                        var rows = boards
                            .Select(d => Ui.CreateBoardPanel(d))
                            .ToArray();

                        var splitRows = new Columns(rows);
                        splitRows.Collapse();

                        var withResult = boards.Count(d => d.GameResult is not null);

                        var statusPanel =
                            new Panel(
                                    $"[blue]Gra w toku...[/] Pozostało do zebrania: {withResult} / {boardStorage.Count}")
                                .Border(BoxBorder.Heavy);

                        ctx.UpdateTarget(new Rows(splitRows,
                            new Rule(),
                            statusPanel
                        ));

                        await Task.Delay(500, cancellationToken);

                    } while (!task.IsCompleted && !cancellationToken.IsCancellationRequested);

                    await task;

                    while (System.Console.KeyAvailable)
                    {
                        var _ = System.Console.ReadKey(intercept: true);
                    }

                    do
                    {
                        var finalBoards = boardStorage.GetAll().ToList();
                        var sortedResults = finalBoards.Where(b => b.GameResult != null)
                            .OrderBy(b => b.GameResult!.Duration).ToList();
                        var resultTable = new Table().RoundedBorder().AddColumn("Miejsce").AddColumn("Board")
                            .AddColumn("Czas (ms)");
                        var place = 1;
                        foreach (var b in sortedResults)
                        {
                            resultTable.AddRow(place.ToString(), b.Address.ToString(),
                                b.GameResult!.Duration.TotalMilliseconds.ToString());
                            place++;
                        }

                        var endPanel = new Panel(new Rows(
                            new FigletText("Wyniki!"),
                            resultTable,
                            new Markup("\nWciśnij [yellow]dowolny klawisz[/], aby powrócić do Lobby...")
                        )).Border(BoxBorder.Double).Expand();

                        var boards = boardStorage.GetAll().ToList();

                        var rows = boards
                            .Select(d => Ui.CreateBoardPanel(d))
                            .ToArray();

                        var splitRows = new Columns(rows);
                        splitRows.Collapse();

                        ctx.UpdateTarget(new Rows(splitRows,
                            new Rule(),
                            endPanel
                        ));

                        if (!System.Console.KeyAvailable)
                        {
                            await Task.Delay(100, cancellationToken);
                        }
                        else
                        {
                            break;
                        }
                    } while (!cancellationToken.IsCancellationRequested);

                    System.Console.Clear();
                } while (!cancellationToken.IsCancellationRequested);
            });
    }

    static void ClearInputBufferBeforeAwaitingPlayerStatus()
    {
        while (System.Console.KeyAvailable)
        {
            var _ = System.Console.ReadKey(intercept: true);
        }
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
    }

    private async Task<int> RunWebServer(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

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

public static class Ui
{
    public static Renderable CreateBoardPanel(Board board)
    {
        board.GetHealthStatus(out var lastHeathCheck, out var duration);
        var lastSeen = lastHeathCheck != null ? (DateTimeOffset.UtcNow - lastHeathCheck!).Value.TotalSeconds : 0;
        var ping = duration?.TotalMilliseconds ?? 0;

        var pingColor = ping < 50 ? "green" : (ping < 150 ? "yellow" : "red");
        var healthText = $"Ostatnie połączenie [white]{lastSeen:F0} sek[/] temu (zajęło [{pingColor}]{ping:F0} ms[/])";

        string statusText;
        if (board.RequestedGameStartOn is null)
        {
            statusText = "[gray]Oczekiwanie na rozpoczęcie gry...[/]";
        }
        else
        {
            statusText = $"[yellow]Gra w toku od {DateTimeOffset.Now - board.RequestedGameStartOn}...[/]";
        }

        var hasResult = board.GameResult is not null;
        if (hasResult)
        {
            statusText = $"[green]Otrzymano wyniki! (zajęło {board.GameResult!.Duration:m\\:ss})[/]";
        }

        var rows = new Rows(
            new Markup(healthText),
            new Markup(string.Empty),
            new Markup(statusText),
            new Markup(board.DescriptionStatus ?? string.Empty)
        );

        var borderStyle = hasResult ? Style.Parse("green") : (ping < 150 ? Style.Parse("blue") : Style.Parse("red"));

        var panel = new Panel(rows)
            .Header(new PanelHeader(board.Address.ToString(), Justify.Left))
            .BorderColor(borderStyle.Foreground)
            .Padding(2, 1);

        return panel;
    }
}
