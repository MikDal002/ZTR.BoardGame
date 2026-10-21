using StreamJsonRpc;
using System.Net.Sockets;

namespace ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

public abstract class RpcConnectionManager<TClient> where TClient : class, IRpcClient
{
    protected JsonRpc HandleNewConnection(TcpClient tcpClient, out TClient clientProxy)
    {
        var networkStream = tcpClient.GetStream();

        var rpcTarget = CreateSessionObject();
        var rpc = JsonRpc.Attach(networkStream, rpcTarget);

        clientProxy = rpc.Attach<TClient>();

        rpc.Disconnected += (s, e) =>
        {
            tcpClient.Dispose();
        };

        return rpc;
    }

    protected abstract object CreateSessionObject();
}
