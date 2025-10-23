using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
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
    private readonly IAnsiConsole _console;

    public HelloService(IOptions<NetworkSettings> networkSettings, HttpClient httpClient, ILogger<HelloService> logger, IAnsiConsole console)
    {
        _networkSettings = networkSettings.Value;
        _httpClient = httpClient;
        _logger = logger;
        _console = console;
    }

    public async Task AnnouncePresence(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_networkSettings.PcServerAddress))
        {
            const string errorMessage = "PC server address is not configured";
            _console.MarkupLine($"[red]Error:[/] {errorMessage}");
            throw new InvalidOperationException(errorMessage);
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
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Announcement task was canceled.");
                break;
            }

            await Task.Delay(10000, cancellationToken);
        }
    }
}
