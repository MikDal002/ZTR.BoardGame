using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Spectre.Console;
using Spectre.Console.Testing;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.Tests.Infrastructure;

namespace ZtrBoardGame.Console.Tests.Features.StepDefinitions;

[Binding]
public class FromPcToBoardStepDefinitions
{
    private CustomWebApplicationFactory<PcRunCommand> _pcServerFactory;
    private IBoardStorage _boardStorage = new BoardStorage();
    private IHelloService _helloService;
    private TestConsole _pcConsole = new();
    private TestConsole _boardConsole = new();
    private CancellationTokenSource _cancellationTokenSource;
    HttpClient _toPcHttpConnection;
    Mock<IHttpClientFactory> _mockHttpClientFactory;

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

    [Given(@"a board is configured to connect to a running PC server")]
    public void GivenABoardIsConfiguredToConnectToARunningPCServer()
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


    [Given(@"a board becomes unreachable after sending its request")]
    public void GivenABoardBecomesUnreachableAfterSendingItsRequest()
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection timed out"));
        var mockHttpClient = new HttpClient(mockMessageHandler.Object);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient);
    }

    [When(@"the PC receives a ""hello"" request from Board")]
    public void WhenThePCReceivesAHelloRequestFromBoard()
    {
        _boardStorage.Add(new Uri("http://127.0.0.1:8080"));
    }

    [When(@"the PC attempts to send a ""hello"" request back")]
    public async Task WhenThePCAttemptsToSendAHelloRequestBack()
    {
        var action = async () =>
        {
            var boardConnectionChecker = new BoardConnectionCheckerService(_mockHttpClientFactory.Object, _pcConsole,
                _boardStorage, NullLogger<BoardConnectionCheckerService>.Instance);
            await boardConnectionChecker.CheckPresenceAsync(_cancellationTokenSource.Token);
        };

        action.Should().ThrowAsync<HttpRequestException>();
    }

    [Then(@"the PC's console should contain an message like ""(.*)""")]
    public void ThenThePCsConsoleShouldContainAnMessageLike(string message)
    {
        _pcConsole.Lines.Should().Contain(str => str.Contains(message));
    }
}
