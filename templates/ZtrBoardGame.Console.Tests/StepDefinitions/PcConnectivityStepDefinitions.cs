using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.Tests.Infrastructure;

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class PcConnectivityStepDefinitions
{
    private CustomWebApplicationFactory<PcRunCommand> _pcServerFactory;
    private IBoardStorage _boardStorage = new BoardStorage();
    private IHelloService _helloService;
    private TestConsole _pcConsole = new();
    private TestConsole _boardConsole = new();
    private CancellationTokenSource _cancellationTokenSource;
    HttpClient _toPcHttpConnection;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _cancellationTokenSource = new();

        _pcServerFactory = new("pc run");
        _pcServerFactory.ConfigureTestServices(services =>
        {
            services.AddSingleton(_boardStorage);
            services.AddSingleton<IAnsiConsole>(_pcConsole);
        });
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _pcServerFactory.Dispose();
        _cancellationTokenSource.Cancel();

    }

    [Given(@"a board is configured with the PC server address")]
    public void GivenABoardIsConfiguredWithThePCServerAddress()
    {
        _toPcHttpConnection = _pcServerFactory.CreateClient();

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(BoardHttpClientConfigure.ToPcClientName))
            .Returns(_toPcHttpConnection);
        _helloService = new HelloService(mockHttpClientFactory.Object, NullLogger<HelloService>.Instance, _boardConsole,
            new ServerAddressStorage() { ServerAddress = "http://dummy-address-for-test:55556" });
    }

    [Given(@"the PC server is running")]
    public static void GivenThePCServerIsRunning()
    {
    }

    [When(@"the board sends a ""hello"" request to the PC from it's IP")]
    public async Task WhenTheBoardSendsAHelloRequestToThePCFromItsIP()
    {
        await _helloService.AnnouncePresenceAsync(CancellationToken.None);
    }

    [When(@"the PC receives the request")]
    public async Task WhenThePCReceivesTheRequest()
    {
        var async = await _toPcHttpConnection.GetAsync("api/boards");
        var readAsStringAsync = await async.Content.ReadAsStringAsync();
        readAsStringAsync.Should().Be(@"{""count"":1}");
    }

    [Then(@"the PC's console log should contain message like ""(.*)""")]
    public void ThenThePCsConsoleLogShouldContainAnInfoMessageLike(string message)
    {
        _pcConsole.Lines.Should().Contain(message);
    }
}
