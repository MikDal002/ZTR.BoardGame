using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Net;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class BoardConnectivityStepDefinitions
{
    private IServiceCollection _services;
    private CancellationTokenSource _cancellationTokenSource;
    private ManualResetEvent _requestReceivedEvent;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private IHelloService _helloService;
    private TestConsole _console;
    ServiceProvider _serviceProvider;

    #region Hooks
    [BeforeScenario]
    public void BeforeScenario()
    {
        _services = new ServiceCollection();
        _cancellationTokenSource = new();
        _requestReceivedEvent = new(false);
        _httpMessageHandlerMock = new();
        _console = new();

        _services.AddSingleton<ILogger<HelloService>>(NullLogger<HelloService>.Instance);
        _services.AddSingleton<IServerAddressProvider>(new ServerAddressStorage() { ServerAddress = "http://dummy-address-for-test" });
        _services.AddSingleton<IHelloService, HelloService>();
        _services.AddSingleton<IAnsiConsole>(_console);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _cancellationTokenSource.Cancel();
    }
    #endregion

    #region Scenario: Board starts with a valid server address configuration
    [Given(@"the board's configuration specifies the PC server address as ""(.*)""")]
    public void GivenTheBoardsConfigurationSpecifiesThePCServerAddressAs(string pcServerAddress)
    {
        var networkSettings = new NetworkSettings { PcServerAddress = pcServerAddress };
        _services.AddSingleton(Options.Create(networkSettings));
    }

    [When(@"the board application starts")]
    public void WhenTheBoardApplicationStarts()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(""),
            })
            .Callback(() => _requestReceivedEvent.Set());

        _services.ConfigureHelloServiceHttpClient(_httpMessageHandlerMock.Object);

        _serviceProvider = _services.BuildServiceProvider();

    }

    [Then(@"the application should run without startup errors")]
    public void ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        _helloService = _serviceProvider.GetRequiredService<IHelloService>();
        var _announcementTask = _helloService.AnnouncePresenceAsync(_cancellationTokenSource.Token);

        _announcementTask.Wait(TimeSpan.FromSeconds(1));
        _announcementTask.IsFaulted.Should().BeFalse();
    }

    [Then(@"the board should begin its announcement cycle to ""(.*)""")]
    public void ThenTheBoardShouldBeginItsAnnouncementCycleTo(string _)
    {
        _requestReceivedEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
    }
    #endregion

    #region Scenario: Board starts without a server address configuration
    [Given(@"the board's configuration does not specify the PC server address")]
    public void GivenTheBoardsConfigurationDoesNotSpecifyThePCServerAddress()
    {
        var networkSettings = new NetworkSettings { PcServerAddress = string.Empty };
        _services.AddSingleton(Options.Create(networkSettings));
    }

    [Then(@"the application should fail to start")]
    public async Task ThenTheApplicationShouldFailToStart()
    {
        var action = () =>
        {
            var helloService = _serviceProvider.GetRequiredService<IHelloService>();
            return helloService.AnnouncePresenceAsync(CancellationToken.None);
        };
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Then(@"an error message ""(.*)"" should be displayed in the console")]
    public void ThenAnErrorMessageShouldBeDisplayedInTheConsole(string errorMessage)
    {
        _console.Output.Should().Contain(errorMessage);
    }
    #endregion

    #region Scenario: A board fails to connect to the PC server
    [Given(@"a board is configured with the PC server address")]
    public void GivenABoardIsConfiguredWithThePCServerAddress()
    {
        GivenTheBoardsConfigurationSpecifiesThePCServerAddressAs("http://localhost:12345");
    }

    [Given(@"the PC server is not reachable")]
    public void GivenThePCServerIsNotReachable()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));
    }

    [When(@"the board attempts to send a ""(.*)"" request")]
    public void WhenTheBoardAttemptsToSendARequest(string _)
    {
        _services.ConfigureHelloServiceHttpClient(_httpMessageHandlerMock.Object);
        _serviceProvider = _services.BuildServiceProvider();
    }

    [Then(@"the board's local console log should contain an ERROR message with a reason, such as ""(.*)"" or ""(.*)""")]
    public async Task ThenTheBoardsLocalConsoleLogShouldContainAnERRORMessageWithAReasonSuchAsOr(string expectedErrorMessage1, string expectedErrorMessage2)
    {
        var helloService = _serviceProvider.GetRequiredService<IHelloService>();
        try
        {
            await helloService.AnnouncePresenceAsync(CancellationToken.None);
        }
        catch
        {
            //  ignored
        }
        _console.Output.Should().ContainAny(expectedErrorMessage1, expectedErrorMessage2);
    }
    #endregion
}
