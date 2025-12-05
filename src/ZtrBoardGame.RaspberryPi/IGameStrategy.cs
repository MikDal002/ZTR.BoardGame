namespace ZtrBoardGame.RaspberryPi;

public interface IGameStrategy
{
    Task<TimeSpan> Do(FieldOrder order);
}
