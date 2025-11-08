using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.PC;

public interface IBoardConnectionCheckerService
{
    Task CheckPresenceAsync(CancellationToken cancellationToken);
}

public class BoardConnectionCheckerService(IHttpClientFactory httpClientFactory, IAnsiConsole console, IBoardStorage boardStorage, ILogger<BoardConnectionCheckerService> logger)
    : IBoardConnectionCheckerService, IHostedService
{
    private const int REPEAT_TO_BOARD_CONNECTION = 10;
    private static readonly TimeSpan REPEAT_TO_BOARD_DELAY = TimeSpan.FromSeconds(1);
    Task _backgroundTask;

    public async Task CheckPresenceAsync(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var boardAddress in boardStorage.GetAllAddresses())
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = boardAddress;
                using var _ =
                    logger.BeginScopeWith(("BoardAddress", httpClient.BaseAddress.ToString()));

                await InvokeWithRetryAsync(async () =>
                {
                    var response = await httpClient.GetAsync("/api/health",  cancellationToken);
                    response.EnsureSuccessStatusCode();
                    logger.LogInformation("Successfully checked connection to Board");
                }, cancellationToken, httpClient.BaseAddress);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private async Task InvokeWithRetryAsync(Func<Task> func, CancellationToken cancellationToken,
        Uri? httpClientBaseAddress)
    {
        var trials = REPEAT_TO_BOARD_CONNECTION;

        while (!cancellationToken.IsCancellationRequested)
        {
            Exception? prevException = null;
            try
            {
                await func();
                return;
            }
            catch (HttpRequestException e)
            {
                console.MarkupLine($"[red]Cannot connect to the board {httpClientBaseAddress}[/]");
                logger.LogError(e, "Failed to connect with Board");

                prevException = e;
            }
            catch (TaskCanceledException e)
            {
                logger.LogInformation("Announcement task was canceled.");
                prevException = e;
                break;
            }
            catch (Exception e)
            {
                prevException = e;
            }
            finally
            {
                trials--;
                if (trials <= 0)
                {
                    throw new("Failed to check connection to Board server over 10 times.", prevException);
                }

                await Task.Delay(REPEAT_TO_BOARD_DELAY, cancellationToken);
            }
        }
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
