using System.Collections.Concurrent;

namespace ZtrBoardGame.Server.Commons;

public interface IBoardStorage
{
    void TryAdd(Board board);
    int Count { get; }
    IEnumerable<Uri> GetAllAddresses();
    Board Get(Uri uri);
    IEnumerable<Board> GetAll();
    bool TryGet(Uri uri, out Board board);
}

// 
// This code might be a cause of security issue https://github.com/MikDal002/ZTR.BoardGame/issues/9
//
public class BoardStorage : IBoardStorage
{
    readonly ConcurrentDictionary<Uri, Board> _connectedBoards = [];

    public void TryAdd(Board board)
        => _connectedBoards.TryAdd(board.Address, board);

    public int Count
        => _connectedBoards.Count;

    public IEnumerable<Uri> GetAllAddresses()
        => _connectedBoards.Keys;

    public Board Get(Uri uri)
        => _connectedBoards.TryGetValue(uri, out var board) ? board : throw new KeyNotFoundException($"Board with address {uri} not found.");

    public bool TryGet(Uri uri, out Board board)
        => _connectedBoards.TryGetValue(uri, out board);

    public IEnumerable<Board> GetAll()
        => _connectedBoards.Values.ToList();
}
