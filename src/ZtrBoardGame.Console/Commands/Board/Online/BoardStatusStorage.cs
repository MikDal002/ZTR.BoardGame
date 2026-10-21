namespace ZtrBoardGame.Console.Commands.Board.Online;

public interface IBoardStatusStorage
{
    bool WasHelloReceived { get; set; }
}

public class BoardStatusStorage : IBoardStatusStorage
{
    public bool WasHelloReceived { get; set; } = false;
}
