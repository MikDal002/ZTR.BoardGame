using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ZtrBoardGame.Testing.ToxiProxy;

/// <summary>
/// Representation of a proxy registered in Toxiproxy.
/// </summary>
public record ToxiProxyDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("listen")] string Listen,
    [property: JsonPropertyName("upstream")] string Upstream,
    [property: JsonPropertyName("enabled")] bool Enabled);

/// <summary>
/// Modern, lightweight HTTP client for the Toxiproxy REST API.
/// Uses System.Net.Http.Json without any outdated third-party libraries.
/// </summary>
public class ToxiProxyClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    public ToxiProxyClient(HttpClient httpClient, bool disposeClient = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeClient = disposeClient;
    }

    public ToxiProxyClient(Uri baseUri) : this(new HttpClient { BaseAddress = baseUri }, disposeClient: true)
    {
    }

    public ToxiProxyClient(string host, int port) : this(new Uri($"http://{host}:{port}"))
    {
    }

    /// <summary>
    /// Creates a new TCP proxy in Toxiproxy.
    /// </summary>
    /// <param name="name">Unique name of the proxy.</param>
    /// <param name="listenPort">The port inside the container where Toxiproxy will listen.</param>
    /// <param name="upstreamPort">The target service port.</param>
    /// <param name="upstreamHost">The target service host (defaults to host.testcontainers.internal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ToxiProxyDto> CreateProxyAsync(
        string name,
        int listenPort,
        int upstreamPort,
        string upstreamHost = ToxiProxyHelper.HostAddress,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            name,
            listen = $"0.0.0.0:{listenPort}",
            upstream = $"{upstreamHost}:{upstreamPort}",
            enabled = true
        };

        var response = await _httpClient.PostAsJsonAsync("/proxies", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ToxiProxyDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize proxy response.");
    }

    /// <summary>
    /// Disables a proxy, dropping active connections and refusing new ones.
    /// </summary>
    public async Task DisableProxyAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/proxies/{name}", new { enabled = false }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Re-enables a previously disabled proxy.
    /// </summary>
    public async Task EnableProxyAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/proxies/{name}", new { enabled = true }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Injects a reset_peer toxic (sends TCP RST packet) on the specified proxy.
    /// </summary>
    public async Task AddResetPeerToxicAsync(
        string proxyName,
        string toxicName = "reset-peer",
        int timeoutMs = 0,
        string stream = "downstream",
        double toxicity = 1.0,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            name = toxicName,
            type = "reset_peer",
            stream,
            toxicity,
            attributes = new
            {
                timeout = timeoutMs
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"/proxies/{proxyName}/toxics", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Injects a timeout toxic (drops all data, simulating hanging connection) on the specified proxy.
    /// </summary>
    public async Task AddTimeoutToxicAsync(
        string proxyName,
        string toxicName = "timeout",
        int timeoutMs = 0,
        string stream = "downstream",
        double toxicity = 1.0,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            name = toxicName,
            type = "timeout",
            stream,
            toxicity,
            attributes = new
            {
                timeout = timeoutMs
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"/proxies/{proxyName}/toxics", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Injects a latency toxic (adds latency and optional jitter) on the specified proxy.
    /// </summary>
    public async Task AddLatencyToxicAsync(
        string proxyName,
        int latencyMs,
        int jitterMs = 0,
        string toxicName = "latency",
        string stream = "downstream",
        double toxicity = 1.0,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            name = toxicName,
            type = "latency",
            stream,
            toxicity,
            attributes = new
            {
                latency = latencyMs,
                jitter = jitterMs
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"/proxies/{proxyName}/toxics", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Resets all proxies to enabled state and removes all active toxics.
    /// </summary>
    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/reset", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes a specific proxy by name.
    /// </summary>
    public async Task DeleteProxyAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/proxies/{name}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}
