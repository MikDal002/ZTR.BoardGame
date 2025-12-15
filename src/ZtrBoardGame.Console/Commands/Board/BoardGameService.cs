using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

internal sealed class BoardGameService(IBoardGameStatusStorage boardGameStatusStorage, IAnsiConsole console, IGameStarter gameStarter, IResultSender resultSender, IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    Task _backgroundTask;
    private readonly CancellationTokenSource _canceler = new();

    public async Task MainGameLoop(CancellationToken cancellationToken)
    {
        do
        {
            await SingleGame(cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }

    async Task SingleGame(CancellationToken cancellationToken)
    {
        await WaitGameToBegin(cancellationToken);

        using var serviceScope = serviceProvider.CreateScope();
        var gameStrategy = serviceScope.ServiceProvider.GetRequiredService<IGameStrategy>();
        var delay = await gameStrategy.Do(boardGameStatusStorage.Get().FieldOrder);

        await FinishTheGame(cancellationToken, delay);
    }

    async Task WaitGameToBegin(CancellationToken cancellationToken)
    {
        await gameStarter.WaitForGameToBeginAsync(cancellationToken);

        console.MarkupLine("[green]Gra rozpoczęta![/]");
    }

    async Task FinishTheGame(CancellationToken cancellationToken, TimeSpan delay)
    {
        console.MarkupLine("[green]Gra zakończona![/]");

        await resultSender.SendResultsAsync(delay, cancellationToken);

        boardGameStatusStorage.Set(StatusRecord.NotStarted);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = MainGameLoop(_canceler.Token);

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
        _backgroundTask.Dispose();
        _canceler.Dispose();
    }
}
