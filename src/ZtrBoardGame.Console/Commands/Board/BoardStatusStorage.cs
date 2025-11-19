namespace ZtrBoardGame.Console.Commands.Board;

public interface IBoardStatusStorage
{
    bool WasHelloReceived { get; set; }
}

public class BoardStatusStorage : IBoardStatusStorage
{
    public bool WasHelloReceived { get; set; } = false;
}
