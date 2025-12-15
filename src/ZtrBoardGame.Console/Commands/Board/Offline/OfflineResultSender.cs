using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Board.Offline;

class OfflineResultSender(IAnsiConsole console) : IResultSender
{
    public Task SendResultsAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        console.MarkupLine($"[yellow]Your result: {delay}[/]");
        return Task.CompletedTask;
    }
}
