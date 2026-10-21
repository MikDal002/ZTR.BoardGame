using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Server.Commons;

namespace ZtrBoardGame.Console.Commands.PC.UI;

public interface ILiveGameDashboard
{
    Task NewPrompting(CancellationToken cancellationToken);
}

class LiveGameDashboard(IGameService gameService, IBoardStorage boardStorage) : ILiveGameDashboard
{
    public async Task NewPrompting(CancellationToken cancellationToken)
    {
        do
        {
            RejectAllAwaitingInput();

            await DrawLobby(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await gameService.StartSessionAsync(cancellationToken);
            var task = gameService.AwaitResultsFromBoards(cancellationToken);

            await DrawDuringTheGame(cancellationToken, task);

            await task;

            RejectAllAwaitingInput();

            await DrawResults(cancellationToken);

            System.Console.Clear();
        } while (!cancellationToken.IsCancellationRequested);
    }

    static void RejectAllAwaitingInput()
    {
        while (System.Console.KeyAvailable)
        {
            var _ = System.Console.ReadKey(intercept: true);
        }
    }

    async Task DrawResults(CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(new Text("Obliczanie wyników..."))
            .StartAsync(async ctx =>
            {
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

                    var splitRows = CreateBoardsLayout(boards);

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
            });
    }

    async Task DrawDuringTheGame(CancellationToken cancellationToken, Task task)
    {
        await AnsiConsole.Live(new Text("Inicjalizacja gry..."))
            .StartAsync(async ctx =>
            {
                do
                {
                    var boards = boardStorage.GetAll().ToList();

                    var splitRows = CreateBoardsLayout(boards);

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
            });
    }

    async Task DrawLobby(CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(new Text("Oczekiwanie na graczy..."))
            .StartAsync(async ctx =>
            {
                var gameStarted = false;
                var errorMessage = "";

                do
                {

                    var boards = boardStorage.GetAll().ToList();
                    foreach (var board in boards)
                    {
                        board.Reset();
                    }

                    var rowsWithBoards = !boards.Any()
                        ? new Panel("[grey]Oczekiwanie na połączenie przynajmniej jednej planszy...[/]")
                            .Border(BoxBorder.None)
                            .Padding(0, 1)
                        : CreateBoardsLayout(boards);

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
            });
    }

    private static IRenderable CreateBoardsLayout(System.Collections.Generic.IEnumerable<Server.Commons.Board> boards)
    {
        var rows = boards
            .Select(d => CreateBoardPanel(d))
            .ToArray();

        var splitRows = new Columns(rows);
        splitRows.Collapse();
        return splitRows;
    }

    private static Renderable CreateBoardPanel(Server.Commons.Board board)
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
