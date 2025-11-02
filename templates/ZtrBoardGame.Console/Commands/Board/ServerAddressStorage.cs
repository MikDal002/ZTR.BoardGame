namespace ZtrBoardGame.Console.Commands.Board;

public interface IServerAddressProvider
{
    public string ServerAddress { get; }
}

public interface IServerAddressSetter
{
    public string ServerAddress { set; }
}

public record ServerAddressStorage : IServerAddressProvider, IServerAddressSetter
{
    public string ServerAddress { get; set; }
}
