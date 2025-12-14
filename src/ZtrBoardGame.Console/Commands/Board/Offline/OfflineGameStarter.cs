using Spectre.Console;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Board.Offline;

class OfflineGameStarter(IAnsiConsole console, IBoardGameStatusStorage boardGameStatusStorage) : IGameStarter
{
    private const int FieldsOnBoard = 16;
    private const int FieldsInPlay = 4;
    public async Task WaitForGameToBeginAsync(CancellationToken cancellationToken)
    {
        var _ = await console.ConfirmAsync("[yellow]Naciśnij dowolny klawisz, aby rozpocząć grę...[/]", cancellationToken: cancellationToken);

        var list = Enumerable.Range(0, FieldsOnBoard)
            .OrderBy(_ => Random.Shared.Next())
            .Take(FieldsInPlay)
            .ToList();
        boardGameStatusStorage.Set(StatusRecord.Started(new(list)));
    }
}
