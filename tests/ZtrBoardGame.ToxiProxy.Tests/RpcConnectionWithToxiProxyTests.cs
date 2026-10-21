using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Sockets;
using ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;
using ZtrBoardGame.Testing.ToxiProxy;

namespace ZtrBoardGame.ToxiProxy.Tests;

/// <summary>
/// Integration tests for RPC connections routed through ToxiProxy.
/// Based on the original <c>RpcConnectionTests</c> from ZtrBoardGame.Console.Tests.
/// </summary>
[TestFixture]
[Category("Integration")]
public class RpcConnectionWithToxiProxyTests
{
    private ServiceProvider _serviceProvider = null!;
    private BoardSideRpcManager _board = null!;
    private ServerSideRpcManager _server = null!;
    private int _serverPort;
    private ToxiProxyClient _toxiClient = null!;

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [SetUp]
    public void SetUp()
    {
        var serviceContainer = new ServiceCollection();
        serviceContainer.AddTransient<IToBoardClient, MockedToBoardClient>();
        serviceContainer.AddTransient<IToServerClient, MockedToServerClient>();

        _serviceProvider = serviceContainer.BuildServiceProvider();
        _board = new BoardSideRpcManager(_serviceProvider, NullLogger<BoardSideRpcManager>.Instance);
        _server = new ServerSideRpcManager(_serviceProvider);
        _serverPort = GetFreePort();
        _toxiClient = ToxiProxyFixture.Instance.CreateClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            await _toxiClient.ResetAllAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }

        _toxiClient.Dispose();
        await _serviceProvider.DisposeAsync();
    }

    /// <summary>
    /// Exact replica of METHOD() from RpcConnectionTests, routed through a healthy ToxiProxy proxy.
    /// Passes just like the original test.
    /// </summary>
    [Test]
    public async Task RpcConnection_ThroughToxiProxy_WorksNormally()
    {
        await _server.StartListeningAsync(_serverPort);

        const int proxyPort = ToxiProxyContainerFixture.FirstProxyPort;
        const string proxyName = "rpc-proxy-normal";

        await _toxiClient.CreateProxyAsync(
            name: proxyName,
            listenPort: proxyPort,
            upstreamPort: _serverPort);

        var mappedProxyPort = ToxiProxyFixture.Instance.GetMappedProxyPort(proxyPort);

        var boardTask = await _board.ConnectToServerAsync(
            ToxiProxyFixture.Instance.Hostname,
            mappedProxyPort);

        var act = async () => await boardTask.DestroyAsync();
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("You destroyed me!");
    }

    /// <summary>
    /// Copy of METHOD() from RpcConnectionTests, where ToxiProxy drops the connection.
    /// Fails because the connection is dropped by ToxiProxy.
    /// </summary>
    [Test]
    public async Task RpcConnection_WhenProxyDropped_FailsDueToConnectionDrop()
    {
        await _server.StartListeningAsync(_serverPort);

        const int proxyPort = ToxiProxyContainerFixture.FirstProxyPort + 1;
        const string proxyName = "rpc-proxy-dropped";

        await _toxiClient.CreateProxyAsync(
            name: proxyName,
            listenPort: proxyPort,
            upstreamPort: _serverPort);

        var mappedProxyPort = ToxiProxyFixture.Instance.GetMappedProxyPort(proxyPort);

        var boardTask = await _board.ConnectToServerAsync(
            ToxiProxyFixture.Instance.Hostname,
            mappedProxyPort);

        // Drop the connection via ToxiProxy
        await _toxiClient.DisableProxyAsync(proxyName);

        // Wywołujemy normalnie endpoint Destroy
        var act = async () => await boardTask.DestroyAsync();
        await act.Should().ThrowAsync<StreamJsonRpc.ConnectionLostException>();
    }
}
