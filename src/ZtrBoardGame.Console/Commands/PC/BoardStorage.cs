using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZtrBoardGame.Console.Commands.PC;

public class Board(
    Uri address,
    DateTimeOffset? LastHealthCheck = null,
    TimeSpan? LastHealthCheckDuration = null,
    DateTimeOffset? ResultArrivedOn = null,
    GameResult? gameResult = null)
{
    public Uri Address { get; } = address;
    public GameResult? GameResult { get; private set; } = gameResult;

    public void GetHealthStatus(out DateTimeOffset? lastHeathCheck, out TimeSpan? duration)
    {
        lastHeathCheck = LastHealthCheck ?? null;
        duration = LastHealthCheckDuration ?? null;
    }

    public void SetGameResults(GameResult result, DateTimeOffset timeOfArrival)
    {
        GameResult = result;
        ResultArrivedOn = timeOfArrival;
    }

    public void Reset()
    {
        GameResult = null;
        RequestedGameStartOn = null;
    }

    public void GameStartRequested(DateTimeOffset now)
    {
        RequestedGameStartOn = now;
    }

    public DateTimeOffset? RequestedGameStartOn { get; private set; }

    public void SetDescriptionStatus(string? objMessage) => DescriptionStatus = objMessage;
    public string? DescriptionStatus { get; private set; }
}

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
