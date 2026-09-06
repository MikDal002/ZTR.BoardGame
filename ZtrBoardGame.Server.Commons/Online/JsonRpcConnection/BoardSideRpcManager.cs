using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

public interface IToServerClient : IRpcClient
{
    Task DestroyAsync();
}

public interface IBoardSideRpcManager : IToServerClient
{
    // Task KeepUpConnectionToServerAsync(string ip, int port = 5151, CancellationToken cancellationToken = default);
}

public class BoardSideConnection : IToServerClient
{
    private IToServerClient _realToServerClient;

    public void UpdateConnection(IToServerClient client)
    {
        _realToServerClient = client;
    }

    public Task DestroyAsync()
    {
        return _realToServerClient.DestroyAsync();
    }
}

public class BoardSideRpcManager(IServiceProvider serviceProvider, ILogger<BoardSideRpcManager> logger) : RpcConnectionManager<IToServerClient>, IBoardSideRpcManager
{
    public async Task<IToServerClient> ConnectToServerAsync(string ip, int port = 5151, CancellationToken cancellationToken = default)
    {
        var tcpClient = new TcpClient();
        await EstablishConnection(ip, port, cancellationToken, tcpClient);

        var rpcConnection = HandleNewConnection(tcpClient, out var clientProxy);
        var singletonProxy = new BoardSideConnection();
        singletonProxy.UpdateConnection(clientProxy);

        // KeepUpConnectionToServerAsync(ip, port, cancellationToken);

        return singletonProxy;
    }

    private async Task EstablishConnection(string ip, int port, CancellationToken cancellationToken, TcpClient tcpClient)
    {
        do
        {
            try
            {
                await tcpClient.ConnectAsync(ip, port, cancellationToken);
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot establish connection with server");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 1000)), cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }

    //public async Task KeepUpConnectionToServerAsync(string ip, int port = 5151, CancellationToken cancellationToken = default)
    //{
    //    while (!cancellationToken.IsCancellationRequested)
    //    {
    //        try
    //        {
    //            var tcpClient = new TcpClient();
    //            await tcpClient.ConnectAsync(ip, port, cancellationToken);

    //            // U¿ywamy wspólnej metody z klasy bazowej
    //            await HandleNewConnection(tcpClient).Completion;
    //        }
    //        catch (Exception e)
    //        {
    //            logger.LogError(e, "Problem with server communication!");
    //        }

    //        await Task.Delay(TimeSpan.FromSeconds(2));
    //    }
    //}

    protected override object CreateSessionObject()
        => serviceProvider.GetRequiredService<IToBoardClient>();

    public Task DestroyAsync()
    {
        throw new NotImplementedException("You destroyed me!");
    }
}
