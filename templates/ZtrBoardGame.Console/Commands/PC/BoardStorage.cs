using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZtrBoardGame.Console.Commands.PC;

public interface IBoardStorage
{
    void Add(Uri boardIpAddress);
    int Count { get; }
    IEnumerable<Uri> GetAllAddresses();
}

// 
// This code might be a cause of security issue https://github.com/MikDal002/ZTR.BoardGame/issues/9
//
public class BoardStorage : IBoardStorage
{
    readonly ConcurrentDictionary<Uri, byte> _connectedBoards = [];

    public void Add(Uri boardIpAddress)
        => _connectedBoards.TryAdd(boardIpAddress, (byte)0);

    public int Count
        => _connectedBoards.Count;

    public IEnumerable<Uri> GetAllAddresses()
        => _connectedBoards.Keys;
}
