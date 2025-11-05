using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.Console.Commands.PC;

public class BoardConnectorService : IHostedService, IDisposable
{
    private readonly ILogger<BoardConnectorService> _logger;
    private readonly IBoardStorage _boardStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cancellationTokenSource;


    public BoardConnectorService(ILogger<BoardConnectorService> logger, IBoardStorage boardStorage, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _boardStorage = boardStorage;
        _httpClientFactory = httpClientFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Board Connector Service is starting.");
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        _timerTask = DoWorkAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (_timer is null)
        {
            return;
        }
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                await ProcessBoardsAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Board Connector Service is stopping.");
        }
    }

    public async Task ProcessBoardsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Board Connector Service is working.");
        var httpClient = _httpClientFactory.CreateClient();
        foreach (var address in _boardStorage.GetAddresses())
        {
            try
            {
                // TODO: create a dedicated endpoint for that
                var response = await httpClient.GetAsync(address, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation(responseBody);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e, "Connection to {address} failed", address);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Board Connector Service is stopping.");
        if (_timerTask is null || _cancellationTokenSource is null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        await _timerTask;
        _cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timerTask?.Dispose();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
