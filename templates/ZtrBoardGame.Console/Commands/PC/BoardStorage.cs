using System;
using System.Collections.Concurrent;

namespace ZtrBoardGame.Console.Commands.PC;

public interface IBoardStorage
{
    void Add(Uri boardIpAddress);
    int Count { get; }
}

public class BoardStorage : IBoardStorage
{
    readonly ConcurrentDictionary<Uri, byte> _connectedBoards = [];

    public void Add(Uri boardIpAddress)
        => _connectedBoards.TryAdd(boardIpAddress, (byte)0);

    public int Count => _connectedBoards.Count;
}
