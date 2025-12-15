using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IResultSender
{
    public Task SendResultsAsync(TimeSpan delay, CancellationToken cancellationToken);
}
