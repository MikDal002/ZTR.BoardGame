using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using ZtrBoardGame.Server.Commons.Extensions;
using ZtrBoardGame.Server.Commons.Online;

namespace ZtrBoardGame.Server.Commons;

public interface IGameService
{
    public IReadOnlyDictionary<Board, GameResult> Results { get; }

    Task StartSessionAsync(CancellationToken cancellationToken);
    void RecordResults(Board board);
    bool AreAllResultsReceived();
    Task AwaitResultsFromBoards(CancellationToken cancellationToken);
}

public class GameService(IBoardStorage boardStorage, IAnsiConsole console, IHttpClientFactory httpClientFactory, ILogger<GameService> logger) : IGameService
{
    readonly ConcurrentDictionary<Board, GameResult> _results = new();
    public IReadOnlyDictionary<Board, GameResult> Results => _results;

    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Check Presence", "the board");

    public void RecordResults(Board board)
    {
        _results[board] = board.GameResult!;
    }

    public async Task StartSessionAsync(CancellationToken cancellationToken)
    {
        foreach (var keyValuePair in _results)
        {
            keyValuePair.Key.Reset();
        }

        _results.Clear();

        await RequestStartOnBoards(cancellationToken);
    }

    public async Task AwaitResultsFromBoards(CancellationToken cancellationToken)
    {
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        } while (!AreAllResultsReceived());
    }

    public bool AreAllResultsReceived()
    {
        return boardStorage.Count > 0 && _results.Count == boardStorage.Count;
    }

    async Task RequestStartOnBoards(CancellationToken cancellationToken)
    {
        var fields = Enumerable.Range(0, 16)
            .OrderBy(x => Random.Shared.Next())
            .Take(4)
            .ToList();

        var gameRequest = new GameStartRequest(fields);

        await Parallel.ForEachAsync(boardStorage.GetAll(), cancellationToken, async (board, cancellationToken) =>
        {
            var boardAddress = board.Address;
            var httpClient = httpClientFactory.CreateClient();
            using var _ = logger.BeginScopeWith(("BoardAddress", boardAddress.ToString()));

            await ResilienceHelper.InvokeWithRetryAsync(async () =>
            {
                var response = await httpClient.PostAsJsonAsync(
                    new Uri(boardAddress, "api/board/game"),
                    gameRequest,
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Successfully started game on Board");
                board.GameStartRequested(DateTimeOffset.Now);
                board.SetDescriptionStatus(null);
            }, ResilienceSettings, console, logger, cancellationToken, boardAddress,
                onError: ex =>
                {
                    board.SetDescriptionStatus(ex.Message);
                });

        });
    }
}
