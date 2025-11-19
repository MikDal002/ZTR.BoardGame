using DotNetEnv;
using DotNetEnv.Extensions;
using Reqnroll;

namespace ZtrBoardGame.E2E.Tests;

[Binding]
public class TwoBoardsOnePcStepDefinitions
{
    private static IDictionary<string, string> _env = new Dictionary<string, string>();

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _env = Env.NoEnvVars().Load("e2etests.env").ToDotEnvDictionary();
    }

    [Given(@"a running Docker environment")]
    public static async Task GivenARunningDockerEnvironment()
    {
        // Wait 30 seconds for docker-compose services to be healthy.
        // This is a minimal implementation to get the test running.
        await Task.Delay(System.TimeSpan.FromSeconds(30));
    }

    [When(@"I send a GET request to ""(.*)""")]
    public static async Task WhenISendAGETRequestTo(string _)
    {
        var boardPorts = new[] { int.Parse(_env["BOARD1_PORT"]), int.Parse(_env["BOARD2_PORT"]) };
        using var client = new HttpClient();

        foreach (var port in boardPorts)
        {
            var healthResponse = await client.GetAsync($"http://localhost:{port}/api/board/health");
            healthResponse.EnsureSuccessStatusCode();
            var healthContent = await healthResponse.Content.ReadAsStringAsync();
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(healthContent);
            var wasHelloReceived = jsonDoc.RootElement.GetProperty("wasHelloReceived").GetBoolean();
            wasHelloReceived.Should().BeTrue($"Board at port {port} did not receive hello.");
        }
    }

    [Then(@"the response should contain two boards")]
    public static async Task ThenTheResponseShouldContainTwoBoards()
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
