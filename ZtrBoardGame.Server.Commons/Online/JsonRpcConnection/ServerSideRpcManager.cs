using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;

namespace ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

public interface IServerSideRpcManager
{
}

public class ServerSideRpcManager(IServiceProvider serviceProvider) : RpcConnectionManager<IToBoardClient>, IServerSideRpcManager
{
    public async Task StartListeningAsync(int port = 5151)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Task.Run(async () =>
        {
            while (true)
            {
                var tcpClient = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleNewConnection(tcpClient, out var clientProxy));
            }
        });
    }

    protected override object CreateSessionObject()
        => serviceProvider.GetRequiredService<IToServerClient>();
}
