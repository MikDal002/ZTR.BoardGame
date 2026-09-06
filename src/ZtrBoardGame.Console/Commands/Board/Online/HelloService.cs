using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.Console.Commands.Board.Online;

public interface IHelloService
{
    Task AnnouncePresenceAsync(CancellationToken cancellationToken);
}

public sealed class HelloService(IHttpClientFactory httpClientFactory, IAnsiConsole console, IOptions<BoardNetworkSettings> serverAddressProvider, IBoardGameStatusStorage boardGameStatusStorage, IServiceProvider serviceProvider, ILogger<HelloService> logger)
    : IHelloService, IHostedService, IDisposable
{
    Task? _backgroundTask;
    private readonly CancellationTokenSource _canceler = new();

    public async Task AnnouncePresenceAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serverAddressProvider.Value.BoardAddress))
        {
            logger.LogWarning("Local server address is not set. Cannot announce presence to PC server.");
            console.MarkupLine("[red]PC server address is not configured.[/]");
            throw new InvalidOperationException("Local server address is not set");
        }

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

            do
            {
                try
                {
                    await AnnounceNotConnected(physicalNotificator, cancellationToken);
                    return false;
                }
                catch (Exception e)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    logger.LogError(e, "Cannot connect to pc, keep trying");
                    await AnnounceNotConnected(physicalNotificator, cancellationToken);
                }
            } while (true);

            await AnnounceConnected(physicalNotificator, cancellationToken);
            return true;
        }
        catch (Exception e)
        {

        }

        return false;
    }

    private static async Task AnnounceNotConnected(IPhysicalNotificator physicalNotificator, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 3; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await physicalNotificator.BlinkRedAsync(TimeSpan.FromSeconds(1));
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private static async Task AnnounceConnected(IPhysicalNotificator physicalNotificator, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 3; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await physicalNotificator.BlinkGreenAsync(TimeSpan.FromSeconds(1));
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
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
        if (_backgroundTask is not null)
        {
            await _backgroundTask;
        }
    }

    public void Dispose()
    {
        _backgroundTask?.Dispose();
        _canceler?.Dispose();
    }
}
