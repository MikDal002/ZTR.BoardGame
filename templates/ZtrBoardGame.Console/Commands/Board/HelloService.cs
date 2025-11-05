using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IHelloService
{
    Task AnnouncePresenceAsync(CancellationToken cancellationToken);
}

public class HelloService(IHttpClientFactory httpClientFactory, ILogger<HelloService> logger, IAnsiConsole console, IServerAddressProvider serverAddressProvider)
    : IHelloService, IHostedService
{
    private const int REPEAT_TO_PC_CONNECTION = 10;
    private static readonly TimeSpan REPEAT_TO_PC_DELAY = TimeSpan.FromSeconds(1);
    Task _backgroundTask;

    public async Task AnnouncePresenceAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(BoardHttpClientConfigure.ToPcClientName);

        using var _ = logger.BeginScopeWith(("PcServerAddress", httpClient.BaseAddress?.ToString() ?? "<NULL>"));

        await InvokeWithRetryAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(serverAddressProvider.ServerAddress))
            {
                logger.LogWarning("Local server address is not set. Cannot announce presence to PC server.");
                throw new("Local server address is not set");
            }

            var urlEncode = WebUtility.UrlEncode(serverAddressProvider.ServerAddress);
            var response = await httpClient.PostAsync($"/api/boards?responseAddress={urlEncode}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            console.MarkupLine($"[green]Connected to the server[/]");
            logger.LogInformation("Successfully announced presence to PC server");
        }, cancellationToken, httpClient.BaseAddress);
    }

    private async Task InvokeWithRetryAsync(Func<Task> func, CancellationToken cancellationToken,
        Uri? httpClientBaseAddress)
    {
        var trials = REPEAT_TO_PC_CONNECTION;

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
                console.MarkupLine($"[red]Cannot connect to the server {httpClientBaseAddress}. Reason: {e.Message}[/]");
                logger.LogError(e, "Failed to announce presence to PC server");

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
                    throw new("Failed to announce presence to PC server over 10 times.", prevException);
                }

                await Task.Delay(REPEAT_TO_PC_DELAY, cancellationToken);
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = AnnouncePresenceAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _backgroundTask;
    }
}
