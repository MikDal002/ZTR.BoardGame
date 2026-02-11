using System;
using System.Threading.Tasks;
using ZtrBoardGame.RaspberryPi;
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.Console.Commands.Board;

public class MockedGameStrategy : IGameStrategy, IPhysicalNotificator
{
    public async Task<TimeSpan> Do(FieldOrder order)
    {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(10, 30));
        await Task.Delay(delay);
        return delay;
    }

    public Task BlinkRedAsync(TimeSpan time)
        => Task.Delay(time);

    public Task BlinkGreenAsync(TimeSpan time)
        => Task.Delay(time);

    public void Dispose()
    {

    }
}
