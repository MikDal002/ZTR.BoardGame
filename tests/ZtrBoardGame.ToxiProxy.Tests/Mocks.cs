using ZtrBoardGame.Server.Commons.Online.JsonRpcConnection;

namespace ZtrBoardGame.ToxiProxy.Tests;

/// <summary>
/// Mock implementation of <see cref="IToBoardClient"/> for test DI registration.
/// Required by <see cref="BoardSideRpcManager.CreateSessionObject"/>.
/// </summary>
class MockedToBoardClient : IToBoardClient;

/// <summary>
/// Mock implementation of <see cref="IToServerClient"/> that simply completes successfully.
/// Required by <see cref="ServerSideRpcManager.CreateSessionObject"/> as the RPC target on the server side.
/// </summary>
class MockedToServerClient : IToServerClient
{
    public Task DestroyAsync()
    {
        Console.WriteLine("SERVER RECEIVED DestroyAsync()");
        return Task.FromException(new Exception("You destroyed me!"));
    }
}
