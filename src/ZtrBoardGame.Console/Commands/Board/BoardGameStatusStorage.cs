using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IBoardGameStatusStorage
{
    public StatusRecord Get();
    public void Set(StatusRecord status);
}

public record StatusRecord
{
    private StatusRecord(bool StartGameRequested, FieldOrder FieldOrder, bool HelloServiceEnded)
    {
        this.StartGameRequested = StartGameRequested;
        this.FieldOrder = FieldOrder;
        this.HelloServiceEnded = HelloServiceEnded;
    }

    public bool StartGameRequested { get; private set; }
    public bool HelloServiceEnded { get; private set; }
    public FieldOrder FieldOrder { get; private set; }

    public static StatusRecord NotStarted => new(false, null, false);

    public StatusRecord StartRequested(FieldOrder fieldOrder) => this with { FieldOrder = fieldOrder, StartGameRequested = true };
    public StatusRecord HelloServiceFinished() => this with { HelloServiceEnded = true };

    public bool IsReadyToStart() => StartGameRequested && HelloServiceEnded && FieldOrder is not null;

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
