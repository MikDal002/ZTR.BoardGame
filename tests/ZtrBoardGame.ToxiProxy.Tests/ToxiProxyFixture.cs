using ZtrBoardGame.Testing.ToxiProxy;

namespace ZtrBoardGame.ToxiProxy.Tests;

/// <summary>
/// Assembly-level NUnit fixture that manages the ToxiProxy container lifecycle.
/// Delegates to the reusable <see cref="ToxiProxyContainerFixture"/> from the infrastructure library.
/// </summary>
[SetUpFixture]
public class ToxiProxyFixture
{
    private static ToxiProxyContainerFixture? _fixture;

    /// <summary>The shared ToxiProxy container fixture instance.</summary>
    public static ToxiProxyContainerFixture Instance => _fixture
        ?? throw new InvalidOperationException("ToxiProxy fixture is not initialized. Ensure [SetUpFixture] ran.");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        _fixture = new ToxiProxyContainerFixture();
        await _fixture.StartAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }
    }
}
