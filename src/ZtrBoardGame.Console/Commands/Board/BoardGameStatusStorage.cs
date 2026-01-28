using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IBoardGameStatusStorage
{
    public StatusRecord Get();
    public void Set(StatusRecord status);
}

public record StatusRecord
{
    private StatusRecord(bool StartGameRequested, FieldOrder FieldOrder)
    {
        this.StartGameRequested = StartGameRequested;
        this.FieldOrder = FieldOrder;
    }

    public bool StartGameRequested { get; }
    public FieldOrder FieldOrder { get; }

    public static StatusRecord NotStarted => new(false, null);
    public static StatusRecord Started(FieldOrder fieldOrder) => new(true, fieldOrder);

    public void Deconstruct(out bool StartGameRequested, out FieldOrder FieldOrder)
    {
        StartGameRequested = this.StartGameRequested;
        FieldOrder = this.FieldOrder;
    }
}

public class BoardGameStatusStorage : IBoardGameStatusStorage
{
    private StatusRecord _status = StatusRecord.NotStarted;

    public StatusRecord Get()
        => _status;

    public void Set(StatusRecord status)
        => _status = status;
}
