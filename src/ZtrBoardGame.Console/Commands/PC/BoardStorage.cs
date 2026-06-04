using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZtrBoardGame.Console.Commands.PC;

public interface IBoardStorage
{
    void Add(Board board);
    int Count { get; }
    IEnumerable<Uri> GetAllAddresses();
    Board Get(Uri uri);
    IEnumerable<Board> GetAll();
}

// 
// This code might be a cause of security issue https://github.com/MikDal002/ZTR.BoardGame/issues/9
//
public class BoardStorage : IBoardStorage
{
    readonly ConcurrentDictionary<Uri, Board> _connectedBoards = [];

    public void Add(Board board)
        => _connectedBoards.TryAdd(board.Address, board);

    public int Count
        => _connectedBoards.Count;

    public IEnumerable<Uri> GetAllAddresses()
        => _connectedBoards.Keys;

    public Board Get(Uri uri)
        => _connectedBoards.TryGetValue(uri, out var board) ? board : throw new KeyNotFoundException($"Board with address {uri} not found.");

    public IEnumerable<Board> GetAll()
        => _connectedBoards.Values.ToList();
}
