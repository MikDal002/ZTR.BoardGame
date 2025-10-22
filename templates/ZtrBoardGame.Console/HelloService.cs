using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console
{
    public class HelloService
    {
        private readonly NetworkSettings _networkSettings;
        private readonly HttpClient _httpClient;

        public HelloService(IOptions<NetworkSettings> networkSettings, HttpClient httpClient)
        {
            _networkSettings = networkSettings.Value;
            _httpClient = httpClient;
        }

        public async Task AnnouncePresence(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_networkSettings.PcServerAddress))
            {
                Log.Error("PC server address is not configured");
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.PostAsync($"{_networkSettings.PcServerAddress}/api/hello", null, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    Log.Information("Successfully announced presence to PC server");
                }
                catch (HttpRequestException e)
                {
                    Log.Error(e, "Failed to announce presence to PC server");
                }

                await Task.Delay(10000, cancellationToken);
            }
        }
    }
}
