using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Board.Online;

class OnlineGameStarter(IAnsiConsole console, IBoardGameStatusStorage boardGameStatusStorage) : IGameStarter
{
    public async Task WaitForGameToBeginAsync(CancellationToken cancellationToken)
    {
        do
        {
            console.MarkupLine("[yellow]Czekanie na rozpoczêcie gry przez serwer...[/]");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        } while (!boardGameStatusStorage.Get().StartGameRequested);
    }
}
