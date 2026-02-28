using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console.Commands.Board.Offline;

class OfflineGameStarter(IAnsiConsole console, IBoardGameStatusStorage boardGameStatusStorage, IOptions<PhysicalBoardSettings> options) : IGameStarter
{
    public async Task WaitForGameToBeginAsync(CancellationToken cancellationToken)
    {
        var _ = await console.ConfirmAsync("[yellow]Naciśnij dowolny klawisz, aby rozpocząć grę...[/]", cancellationToken: cancellationToken);

        var list = Enumerable.Range(0, options.Value.AmountOfFields())
            .OrderBy(_ => Random.Shared.Next())
            .Take(options.Value.FieldsInPlay)
            .ToList();

        boardGameStatusStorage.Set(StatusRecord.NotStarted.StartRequested(new(list)).HelloServiceFinished());
    }
}
