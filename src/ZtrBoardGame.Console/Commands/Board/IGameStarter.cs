using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IGameStarter
{
    public Task WaitForGameToBeginAsync(CancellationToken cancellationToken);
}
