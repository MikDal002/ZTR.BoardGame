using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
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

    public async Task CheckPresenceAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
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

        await Parallel.ForEachAsync(boardStorage.GetAllAddresses(), parallelOptions,
            async (boardAddress, ct) => await CheckSingleBoard(ct, boardAddress));
    }

    private async Task CheckSingleBoard(CancellationToken cancellationToken, Uri boardAddress)
    {
        var httpClient = httpClientFactory.CreateClient();
        using var _ = logger.BeginScopeWith(("BoardAddress", boardAddress.ToString()));

        await ResilienceHelper.InvokeWithRetryAsync(async () =>
        {
            var response = await httpClient.GetAsync(new Uri(boardAddress, BoardApiUrl), cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Successfully checked connection to Board");
        }, ResilienceSettings, console, logger, cancellationToken, boardAddress);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = CheckPresenceAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _backgroundTask;
    }
}
