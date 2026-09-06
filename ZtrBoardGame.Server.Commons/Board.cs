namespace ZtrBoardGame.Server.Commons;

public record GameResult(TimeSpan Duration);

public class Board(
    Uri address,
    DateTimeOffset? LastHealthCheck = null,
    TimeSpan? LastHealthCheckDuration = null,
    DateTimeOffset? ResultArrivedOn = null,
    GameResult? gameResult = null)
{
    public Uri Address { get; } = address;
    public GameResult? GameResult { get; private set; } = gameResult;
    public DateTimeOffset? ResultArrivedOn { get; private set; } = ResultArrivedOn;
    public void GetHealthStatus(out DateTimeOffset? lastHeathCheck, out TimeSpan? duration)
    {
        lastHeathCheck = LastHealthCheck;
        duration = LastHealthCheckDuration;
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
