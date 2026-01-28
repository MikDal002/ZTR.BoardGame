using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.Board.Online;

class OnlineResultSender(IAnsiConsole console, IHttpClientFactory httpClientFactory, IOptions<BoardNetworkSettings> serverAddressProvider, ILogger<OnlineResultSender> logger) : IResultSender
{
    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Announce Presence", "the server");

    public async Task SendResultsAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(BoardHttpClientConfigure.ToPcClientName);

        using var _ = logger.BeginScopeWith(("PcServerAddress", httpClient.BaseAddress?.ToString() ?? "<NULL>"));

        await ResilienceHelper.InvokeWithRetryAsync(async () =>
        {
            await SendResults(cancellationToken, delay, httpClient);
        }, ResilienceSettings, console, logger, cancellationToken, httpClient.BaseAddress);
    }

    async Task SendResults(CancellationToken cancellationToken, TimeSpan delay, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(serverAddressProvider.Value.BoardAddress))
        {
            logger.LogWarning("Local server address is not set. Cannot announce presence to PC server.");
            throw new InvalidOperationException("Local server address is not set");
        }

        var urlEncode = WebUtility.UrlEncode(serverAddressProvider.Value.BoardAddress);
        var resultEncoded = WebUtility.UrlEncode(delay.ToString());
        var response = await httpClient.PostAsync(
            $"/api/boards/game/status?responseAddress={urlEncode}&result={resultEncoded}", null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        console.MarkupLine($"[green]Results sent to the server[/]");
        logger.LogInformation("Successfully sent results to the server");
    }
}
