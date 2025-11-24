namespace ZtrBoardGame.Console.Commands.Board;

public interface IBoardGameStatusStorage
{
    public bool StartGameRequested { get; set; }
}

public class BoardGameStatusStorage : IBoardGameStatusStorage
{
    public bool StartGameRequested { get; set; }
}
