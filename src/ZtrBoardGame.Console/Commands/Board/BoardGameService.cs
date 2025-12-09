using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public class BoardGameService(IBoardGameStatusStorage boardGameStatusStorage, IHttpClientFactory httpClientFactory, IAnsiConsole console, IOptions<BoardNetworkSettings> serverAddressProvider,
    IServiceProvider serviceProvider, ILogger<BoardGameService> logger) : IHostedService
{
    private static readonly ResilienceSettings ResilienceSettings = new(10, TimeSpan.FromSeconds(1), "Announce Presence", "the server");
    Task _backgroundTask;

    public async Task MainGameLoop(CancellationToken cancellationToken)
    {
        do
        {
            await SingleGame(cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }

    async Task SingleGame(CancellationToken cancellationToken)
    {
        await WaitGameToBegin();
        using var serviceScope = serviceProvider.CreateScope();
        var gameStrategy = serviceScope.ServiceProvider.GetRequiredService<IGameStrategy>();
        var delay = await gameStrategy.Do(boardGameStatusStorage.FieldOrder);

        await FinishTheGame(cancellationToken, delay);
    }

    async Task WaitGameToBegin()
    {
        do
        {
            AnsiConsole.MarkupLine("[yellow]Czekanie na rozpoczęcie gry...[/]");
            await Task.Delay(TimeSpan.FromSeconds(2));
        } while (!boardGameStatusStorage.StartGameRequested);

        AnsiConsole.MarkupLine("[green]Gra rozpoczęta![/]");

    }

    async Task FinishTheGame(CancellationToken cancellationToken, TimeSpan delay)
    {
        AnsiConsole.MarkupLine("[green]Gra zakończona![/]");

        var httpClient = httpClientFactory.CreateClient(BoardHttpClientConfigure.ToPcClientName);

        using var _ = logger.BeginScopeWith(("PcServerAddress", httpClient.BaseAddress?.ToString() ?? "<NULL>"));

        await ResilienceHelper.InvokeWithRetryAsync(async () =>
        {
            await SendResults(cancellationToken, delay, httpClient);
        }, ResilienceSettings, console, logger, cancellationToken, httpClient.BaseAddress);

        boardGameStatusStorage.StartGameRequested = false;
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = MainGameLoop(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _backgroundTask;
    }
}
