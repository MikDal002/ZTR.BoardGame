using Testcontainers.Toxiproxy;

namespace ZtrBoardGame.Testing.ToxiProxy;

/// <summary>
/// Manages the lifecycle of a ToxiProxy Docker container using Testcontainers.
/// Fully framework-agnostic — can be reused across any test framework (NUnit, xUnit, MSTest).
/// </summary>
public class ToxiProxyContainerFixture : IAsyncDisposable
{
    private ToxiproxyContainer? _container;
    private ToxiProxyClient? _client;

    /// <summary>ToxiProxy control REST API port inside the container.</summary>
    public const int ApiPort = 8474;

    /// <summary>First port in the range available for proxy listeners inside the container.</summary>
    public const int FirstProxyPort = 8475;

    /// <summary>Default number of proxy ports to expose (8475–8485).</summary>
    public const int DefaultProxyPortCount = 11;

    /// <summary>Number of proxy ports exposed.</summary>
    public int ProxyPortCount { get; }

    /// <summary>Hostname of the container (for host-to-container connections).</summary>
    public string Hostname => Container.Hostname;

    /// <summary>The running Toxiproxy container instance.</summary>
    public ToxiproxyContainer Container => _container
        ?? throw new InvalidOperationException("ToxiProxy container is not started. Call StartAsync() first.");

    /// <summary>
    /// Creates a new fixture with the specified number of proxy ports.
    /// </summary>
    /// <param name="proxyPortCount">Number of proxy ports to expose (default: 11, ports 8475–8485).</param>
    public ToxiProxyContainerFixture(int proxyPortCount = DefaultProxyPortCount)
    {
        ProxyPortCount = proxyPortCount;
    }

    /// <summary>
    /// Starts the ToxiProxy container and prepares the client.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_container is not null)
        {
            return;
        }

        var builder = new ToxiproxyBuilder("ghcr.io/shopify/toxiproxy");

        for (var port = FirstProxyPort; port < FirstProxyPort + ProxyPortCount; port++)
        {
            builder = builder.WithPortBinding(port, true);
        }

        _container = builder.Build();
        await _container.StartAsync(cancellationToken);

        var mappedApiPort = _container.GetMappedPublicPort(ApiPort);
        _client = new ToxiProxyClient(Hostname, mappedApiPort);
    }

    /// <summary>
    /// Returns the mapped public port for a proxy port inside the container.
    /// </summary>
    /// <param name="internalPort">Container proxy port (e.g. 8475).</param>
    public ushort GetMappedProxyPort(int internalPort) => Container.GetMappedPublicPort(internalPort);

    /// <summary>
    /// Gets the cached ToxiProxy REST API client.
    /// </summary>
    public ToxiProxyClient GetClient() => _client
        ?? throw new InvalidOperationException("ToxiProxy client is not available. Call StartAsync() first.");

    /// <summary>
    /// Creates a new ToxiProxy REST API client instance.
    /// </summary>
    public ToxiProxyClient CreateClient()
    {
        var mappedApiPort = Container.GetMappedPublicPort(ApiPort);
        return new ToxiProxyClient(Hostname, mappedApiPort);
    }

    /// <summary>
    /// Stops and cleans up the container and client.
    /// </summary>
    public async Task StopAsync()
    {
        _client?.Dispose();
        _client = null;

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
            _container = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}
