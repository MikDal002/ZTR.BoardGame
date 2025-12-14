using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Spectre.Console;
using Spectre.Console.Testing;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.Board.Online;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.Tests.Infrastructure;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Tests.Features.StepDefinitions;

[Binding, Scope(Feature = "From Pc To Board Connection")]
public class FromPcToBoardStepDefinitions
{
    private CustomWebApplicationFactory<PcRunCommand> _pcServerFactory;
    private CustomWebApplicationFactory<BoardRunCommand> _boardServerFactory;
    private readonly IBoardStorage _boardStorage = new BoardStorage();
    private IHelloService _helloService;
    private readonly TestConsole _pcConsole = new();
    private readonly TestConsole _boardConsole = new();
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

        _boardServerFactory = new("board run");
        _boardServerFactory.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAnsiConsole>(_boardConsole);
            services.AddSingleton<IGameStrategy, MockedGameStrategy>();
        });
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _pcServerFactory.Dispose();
        _cancellationTokenSource.Cancel();

    }

    #region Shared Steps
    [Given(@"the PC server is running")]
    public static void GivenThePCServerIsRunning()
    {
        // This is done in another step
    }
    #endregion

    #region Scenario: A healthy board announces its presence and the PC acknowledges it
    [Given(@"a board is configured to connect to a running PC server")]
    public void GivenABoardIsConfiguredToConnectToARunningPCServer()
    {
        _toPcHttpConnection = _pcServerFactory.CreateClient();

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(BoardHttpClientConfigure.ToPcClientName))
            .Returns(_toPcHttpConnection);
        _helloService = new HelloService(mockHttpClientFactory.Object,
            _boardConsole,
           Options.Create(new BoardNetworkSettings() { BoardAddress = "http://dummy-address-for-test:55556" }),
            NullLogger<HelloService>.Instance);
    }

    [When(@"the board sends a ""hello"" request to the PC from it's IP")]
    public async Task WhenTheBoardSendsAHelloRequestToThePCFromItsIP()
    {
        await _helloService.AnnouncePresenceAsync(CancellationToken.None);
    }

    [When(@"the PC receives the request")]
    public async Task WhenThePCReceivesTheRequest()
    {
        var boardsResponse = await _toPcHttpConnection.GetAsync("api/boards");
        var readAsStringAsync = await boardsResponse.Content.ReadAsStringAsync();
        readAsStringAsync.Should().Be(@"{""count"":1}");
    }

    [Then(@"the PC's console log should contain message like ""(.*)""")]
    public void ThenThePCsConsoleLogShouldContainAnInfoMessageLike(string message)
    {
        _pcConsole.Lines.Should().ContainMatch(message + "*");
    }
    #endregion

    #region Scenario: The PC server fails to acknowledge a board
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

        await action.Should().ThrowAsync<TimeoutException>();
    }

    [Then(@"the PC's console should contain an message like ""(.*)""")]
    public void ThenThePCsConsoleShouldContainAnMessageLike(string message)
    {
        _pcConsole.Lines.Should().Contain(str => str.Contains(message));
    }
    #endregion

    #region Scenario: The PC server successfully acknowledges board
    [Given("Board is running")]
    public void GivenBoardIsRunning()
    {
        var toBoardHttpConnection = _boardServerFactory.CreateClient();

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(toBoardHttpConnection);
    }

    [When(@"the PC sends a ""hello"" request to the Board")]
    public async Task WhenThePCSendsAHelloRequestToTheBoard()
    {
        var boardConnectionChecker = new BoardConnectionCheckerService(_mockHttpClientFactory.Object, _pcConsole,
            _boardStorage, NullLogger<BoardConnectionCheckerService>.Instance);
        await boardConnectionChecker.CheckAllBoards(_cancellationTokenSource.Token);
    }

    [Then(@"the Board's console should contain messages like ""(.*)""")]
    public void ThenTheBoardsConsoleShouldContainMessagesLike(string message)
    {
        _boardConsole.Lines.Should().Contain(str => str.Contains(message));
    }
    #endregion

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _cancellationTokenSource?.Dispose();
    }
}
