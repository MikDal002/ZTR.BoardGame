using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console;

public interface IHelloService
{
    Task AnnouncePresence(CancellationToken cancellationToken);
}

public class HelloService : IHelloService
{
    private readonly NetworkSettings _networkSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HelloService> _logger;

    public HelloService(IOptions<NetworkSettings> networkSettings, HttpClient httpClient, ILogger<HelloService> logger)
    {
        _networkSettings = networkSettings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task AnnouncePresence(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_networkSettings.PcServerAddress))
        {
            _logger.LogError("PC server address is not configured");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_networkSettings.PcServerAddress}/api/hello", null, cancellationToken);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Successfully announced presence to PC server");
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Failed to announce presence to PC server");
            }

            await Task.Delay(10000, cancellationToken);
        }
    }
}
