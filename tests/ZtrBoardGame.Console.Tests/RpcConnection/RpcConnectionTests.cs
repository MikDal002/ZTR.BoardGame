using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

namespace ZtrBoardGame.Console.Tests.RpcConnection;

class RpcConnectionTests
{

    [Test]
    public async Task METHOD()
    {
        var serviceContainer = new ServiceCollection();
        serviceContainer.AddTransient<IToBoardClient, MockedToBoardClient>();

        var serviceProvider = serviceContainer.BuildServiceProvider();
        var board = new BoardSideRpcManager(serviceProvider, NullLogger<BoardSideRpcManager>.Instance);

        var server = new ServerSideRpcManager(serviceProvider);

        var serverTask = server.StartListeningAsync(5151);
        var boardTask = await board.ConnectToServerAsync("localhost");

        var act = async () => await boardTask.DestroyAsync();
        act.Should().ThrowAsync<Exception>()
            .WithMessage("You destroyed me!");
    }

    public static void method2()
    {

    }
}

class MockedToBoardClient : IToBoardClient
{
    public MockedToBoardClient()
    {

    }
}
