using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.PC;

public record Board(Uri Address);
public record GameResult(TimeSpan Duration);

public interface IGameService
{
    Task StartSessionAsync(CancellationToken cancellationToken);
    void RecordResults(Board board, GameResult result);
}

public class GameService(IBoardStorage boardStorage, IAnsiConsole console, IHttpClientFactory httpClientFactory, ILogger<GameService> logger) : IGameService
{
    readonly ConcurrentDictionary<Board, GameResult> _results = new();
    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Check Presence", "the board");

    public void RecordResults(Board board, GameResult result)
    {
        console.MarkupLine($"Received game status from board: {board.Address}");
        _results.TryAdd(board, result);
    }

    public async Task StartSessionAsync(CancellationToken cancellationToken)
    {
        foreach (var boardAddress in boardStorage.GetAllAddresses())
        {
            var httpClient = httpClientFactory.CreateClient();
            using var _ = logger.BeginScopeWith(("BoardAddress", boardAddress.ToString()));

            await ResilienceHelper.InvokeWithRetryAsync(async () =>
            {
                var response = await httpClient.PostAsync(new Uri(boardAddress, "api/board/game"), null, cancellationToken);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Successfully checked connection to Board");
            }, ResilienceSettings, console, logger, cancellationToken, boardAddress);
        }

        do
        {
            AnsiConsole.MarkupLine($"[grey]Oczekiwanie na wyniki od graczy[/]");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        } while (_results.Count != boardStorage.Count);

        var sorted = _results.OrderBy(kv => kv.Value.Duration).ToList();

        var table = new Table()
            .RoundedBorder()
            .AddColumn("Miejsce")
            .AddColumn("Board")
            .AddColumn("Czas");

        var place = 1;
        foreach (var kv in sorted)
        {
            var board = kv.Key;
            var result = kv.Value;
            table.AddRow(place.ToString(), board.Address.ToString(), result.Duration.ToString(@"hh\:mm\:ss"));
            place++;
        }

        AnsiConsole.Write(new FigletText("Wyniki"));
        AnsiConsole.Write(table);
    }
}
