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

public class HelloService(IHttpClientFactory httpClientFactory, IAnsiConsole console, IServerAddressProvider serverAddressProvider, ILogger<HelloService> logger)
    : IHelloService, IHostedService
{
    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Announce Presence", "the server");
    Task _backgroundTask;

    public async Task AnnouncePresenceAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(BoardHttpClientConfigure.ToPcClientName);

        using var _ = logger.BeginScopeWith(("PcServerAddress", httpClient.BaseAddress?.ToString() ?? "<NULL>"));

        await ResilienceHelper.InvokeWithRetryAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(serverAddressProvider.ServerAddress))
            {
                logger.LogWarning("Local server address is not set. Cannot announce presence to PC server.");
                throw new InvalidOperationException("Local server address is not set");
            }

            var urlEncode = WebUtility.UrlEncode(serverAddressProvider.ServerAddress);
            var response = await httpClient.PostAsync($"/api/boards?responseAddress={urlEncode}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            console.MarkupLine($"[green]Connected to the server[/]");
            logger.LogInformation("Successfully announced presence to PC server");
        }, ResilienceSettings, console, logger, cancellationToken, httpClient.BaseAddress);
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
