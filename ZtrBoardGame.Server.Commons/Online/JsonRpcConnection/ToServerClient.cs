using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

public interface IToBoardClient : IRpcClient
{

}

public class ToServerClient(IBoardStorage boardStorage, ILogger<ToServerClient> logger) : IToServerClient
{
    Board board = null;

    public Task RegisterBoard(string boardUri)
    {
        if (string.IsNullOrWhiteSpace(boardUri))
        {
            throw new InvalidOperationException("Request received without a remote IP address.");
        }

        var boardIpAddress = new Uri(boardUri);
        logger.LogInformation("Received hello from board {BoardIpAddress}", boardIpAddress);
        if (boardStorage.TryGet(boardIpAddress, out var board))
        {
            this.board = board;
        }
        else
        {
            this.board = new Board(boardIpAddress);
            boardStorage.TryAdd(board);
        }

        return Task.CompletedTask;
    }

    public Task DestroyAsync()
    {
        throw new NotImplementedException("You destroyed me!");
    }
}
