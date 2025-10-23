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

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class BoardConnectivityStepDefinitions
{
    private IServiceCollection _services;
    private CancellationTokenSource _cancellationTokenSource;
    private ManualResetEvent _requestReceivedEvent;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private IHelloService _helloService;
    private Task _announcementTask;
    private TestConsole _console;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _services = new ServiceCollection();
        _cancellationTokenSource = new CancellationTokenSource();
        _requestReceivedEvent = new ManualResetEvent(false);
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _console = new TestConsole();

        _services.AddSingleton<ILogger<HelloService>>(NullLogger<HelloService>.Instance);
        _services.AddSingleton<IAnsiConsole>(_console);
    }

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

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _services.AddSingleton(httpClient);
        _services.AddSingleton<IHelloService, HelloService>();

        var serviceProvider = _services.BuildServiceProvider();
        _helloService = serviceProvider.GetRequiredService<IHelloService>();

        _announcementTask = _helloService.AnnouncePresence(_cancellationTokenSource.Token);
    }

    [Then(@"the application should run without startup errors")]
    public void ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        _announcementTask.Wait(TimeSpan.FromSeconds(1));
        _announcementTask.IsFaulted.Should().BeFalse();
    }

    [Then(@"the board should begin its announcement cycle to ""(.*)""")]
    public void ThenTheBoardShouldBeginItsAnnouncementCycleTo(string _)
    {
        _requestReceivedEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
    }

    [Given(@"the board's configuration does not specify the PC server address")]
    public void GivenTheBoardsConfigurationDoesNotSpecifyThePCServerAddress()
    {
        var networkSettings = new NetworkSettings { PcServerAddress = string.Empty };
        _services.AddSingleton(Options.Create(networkSettings));
    }

    [Then(@"the application should fail to start")]
    public void ThenTheApplicationShouldFailToStart()
    {
        try
        {
            _announcementTask.Wait(_cancellationTokenSource.Token);
        }
        catch (AggregateException)
        {
            // Expected for a faulted task
        }

        _announcementTask.IsFaulted.Should().BeTrue();
        _announcementTask.Exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Then(@"an error message ""(.*)"" should be displayed in the console")]
    public void ThenAnErrorMessageShouldBeDisplayedInTheConsole(string errorMessage)
    {
        _console.Output.Should().Contain(errorMessage);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            _announcementTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (Exception)
        {
            // Ignore exceptions during teardown
        }
    }
}
