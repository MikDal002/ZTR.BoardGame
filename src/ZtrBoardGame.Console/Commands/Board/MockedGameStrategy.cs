using System;
using System.Threading.Tasks;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public class MockedGameStrategy : IGameStrategy
{
    public async Task<TimeSpan> Do(FieldOrder order)
    {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(10, 30));
        await Task.Delay(delay);
        return delay;
    }
}
