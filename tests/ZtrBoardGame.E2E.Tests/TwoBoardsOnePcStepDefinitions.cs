using DotNetEnv;
using DotNetEnv.Extensions;
using Reqnroll;

namespace ZtrBoardGame.E2E.Tests;

[Binding]
public class TwoBoardsOnePcStepDefinitions
{
    readonly static TimeSpan TIMEOUT = TimeSpan.FromSeconds(60);
    private IDictionary<string, string> _env = new Dictionary<string, string>();

    [BeforeScenario]
    public void BeforeScenario()
    {
        _env = Env.NoEnvVars().Load("e2etests.env").ToDotEnvDictionary();
    }

    [Given(@"a running Docker environment")]
    public async Task GivenARunningDockerEnvironment()
    {
        var services = new[]
        {
            $"http://localhost:{_env["BOARD1_PORT"]}/api/board/health",
            $"http://localhost:{_env["BOARD2_PORT"]}/api/board/health",
            $"http://localhost:{_env["PC_SERVER_PORT"]}/api/boards"
        };

        using var client = new HttpClient();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var url in services)
        {
            while (stopwatch.Elapsed < TIMEOUT)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch (HttpRequestException)
                {
                    // Service not ready, wait and retry
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (stopwatch.Elapsed >= TIMEOUT)
            {
                throw new TimeoutException($"Service at {url} did not become healthy within the timeout period.");
            }
        }
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGETRequestTo(string _)
    {
        var boards = new[]
        {
            new { Host = "localhost", Port = _env["BOARD1_PORT"] },
            new { Host = "localhost", Port = _env["BOARD2_PORT"] }
        };
        using var client = new HttpClient();

        foreach (var board in boards)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var wasHelloReceived = false;
            while (stopwatch.Elapsed < TIMEOUT)
            {
                var healthResponse = await client.GetAsync($"http://{board.Host}:{board.Port}/api/board/health");
                healthResponse.EnsureSuccessStatusCode();
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(healthContent);
                wasHelloReceived = jsonDoc.RootElement.GetProperty("wasHelloReceived").GetBoolean();
                if (wasHelloReceived)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            wasHelloReceived.Should().BeTrue($"Board at port {board.Port} did not receive hello within {TIMEOUT.TotalSeconds} seconds.");
        }
    }

    [Then(@"the response should contain two boards")]
    public async Task ThenTheResponseShouldContainTwoBoards()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_env["PC_SERVER_PORT"]}") };
        var response = await httpClient.GetAsync("api/boards");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        var count = jsonDoc.RootElement.GetProperty("count").GetInt32();
        count.Should().Be(2);
    }
}
