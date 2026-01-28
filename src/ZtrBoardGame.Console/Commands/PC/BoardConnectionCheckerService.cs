using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.PC;

public interface IBoardConnectionCheckerService
{
    Task CheckPresenceAsync(CancellationToken cancellationToken);
}

public class BoardConnectionCheckerService(IHttpClientFactory httpClientFactory, IAnsiConsole console, IBoardStorage boardStorage, ILogger<BoardConnectionCheckerService> logger)
    : IBoardConnectionCheckerService, IHostedService
{
    private const string BoardApiUrl = "api/board/health";
    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Check Presence", "the board");
    Task _backgroundTask;
    private bool _stopProcessing = false;

    public async Task CheckPresenceAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Start checking board presence service");
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_stopProcessing)
            {
                return;
            }

            await CheckAllBoards(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    public async Task CheckAllBoards(CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10
        };

        var boards = boardStorage.GetAllAddresses().ToList();
        logger.LogTrace("Boards to check {BboardsAmount}.", boards.Count);

        await Parallel.ForEachAsync(boards, parallelOptions,
            async (boardAddress, ct) => await CheckSingleBoard(ct, boardAddress));
    }

    private async Task CheckSingleBoard(CancellationToken cancellationToken, Uri boardAddress)
    {
        var httpClient = httpClientFactory.CreateClient();
        using var _ = logger.BeginScopeWith(("BoardAddress", boardAddress.ToString()));

        await ResilienceHelper.InvokeWithRetryAsync(async () =>
        {
            if (_stopProcessing)
            {
                return;
            }

            var response = await httpClient.PostAsync(new Uri(boardAddress, BoardApiUrl), null, cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Successfully checked connection to Board");
        }, ResilienceSettings, console, logger, cancellationToken, boardAddress);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_stopProcessing)
        {
            throw new InvalidOperationException("You should create new task, this was stopped");
        }

        _backgroundTask = CheckPresenceAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopProcessing = true;
        await _backgroundTask;
    }
}
