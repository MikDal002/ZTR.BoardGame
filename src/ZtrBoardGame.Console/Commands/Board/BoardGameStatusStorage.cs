using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public interface IBoardGameStatusStorage
{
    bool StartGameRequested { get; set; }
    FieldOrder FieldOrder { get; set; }
}

public class BoardGameStatusStorage : IBoardGameStatusStorage
{
    public bool StartGameRequested { get; set; }
    public FieldOrder FieldOrder { get; set; }
}
