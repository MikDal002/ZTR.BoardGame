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
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.Console.Commands.Board.Online;

public interface IHelloService
{
    Task AnnouncePresenceAsync(CancellationToken cancellationToken);
}

public sealed class HelloService(IHttpClientFactory httpClientFactory, IAnsiConsole console, IOptions<BoardNetworkSettings> serverAddressProvider, IBoardGameStatusStorage boardGameStatusStorage, IServiceProvider serviceProvider, ILogger<HelloService> logger)
    : IHelloService, IHostedService, IDisposable
{
    private static readonly ResilienceSettings ResilienceSettings = new(2, TimeSpan.FromSeconds(1), "Announce Presence", "the server");
    Task _backgroundTask;
    private readonly CancellationTokenSource _canceler = new();

    public async Task AnnouncePresenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var physicalNotificator = serviceProvider.GetRequiredService<IPhysicalNotificator>();

            do
            {
                var isSucessful = await AnnouncePresenceAsync(physicalNotificator, cancellationToken);
                if (isSucessful)
                {
                    return;
                }
            } while (!cancellationToken.IsCancellationRequested);
        }
        finally
        {
            boardGameStatusStorage.Set(boardGameStatusStorage.Get().HelloServiceFinished());
        }
    }

    public async Task<bool> AnnouncePresenceAsync(IPhysicalNotificator physicalNotificator,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(BoardHttpClientConfigure.ToPcClientName);

            using var _ =
                logger.BeginScopeWith(("PcServerAddress", httpClient.BaseAddress?.ToString() ?? "<NULL>"));

            var isConnectionSuccessful = await ResilienceHelper.InvokeWithRetryAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(serverAddressProvider.Value.BoardAddress))
                {
                    logger.LogWarning("Local server address is not set. Cannot announce presence to PC server.");
                    throw new InvalidOperationException("Local server address is not set");
                }

                var urlEncode = WebUtility.UrlEncode(serverAddressProvider.Value.BoardAddress);
                var response = await httpClient.PostAsync($"/api/boards?responseAddress={urlEncode}", null,
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                console.MarkupLine($"[green]Connected to the server[/]");
                logger.LogInformation("Successfully announced presence to PC server");
            }, ResilienceSettings, console, logger, cancellationToken, httpClient.BaseAddress);

            if (!isConnectionSuccessful)
            {
                await AnnounceNotConnected(physicalNotificator);
                return false;
            }

            await AnnounceConnected(physicalNotificator);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot connect to pc, keep trying");
            await AnnounceNotConnected(physicalNotificator);
        }

        return false;
    }

    private static async Task AnnounceNotConnected(IPhysicalNotificator physicalNotificator)
    {
        for (var i = 0; i < 3; i++)
        {
            await physicalNotificator.BlinkRedAsync(TimeSpan.FromSeconds(1));
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private static async Task AnnounceConnected(IPhysicalNotificator physicalNotificator)
    {
        for (var i = 0; i < 3; i++)
        {
            await physicalNotificator.BlinkGreenAsync(TimeSpan.FromSeconds(1));
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = AnnouncePresenceAsync(_canceler.Token);

        if (cancellationToken.IsCancellationRequested)
        {
            return StopAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _canceler.CancelAsync();
        await _backgroundTask;
    }

    public void Dispose()
    {
        _backgroundTask?.Dispose();
        _canceler?.Dispose();
    }
}
