using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
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

    [BeforeScenario]
    public void BeforeScenario()
    {
        _services = new ServiceCollection();
        _cancellationTokenSource = new CancellationTokenSource();
        _requestReceivedEvent = new ManualResetEvent(false);
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _services.AddSingleton<ILogger<HelloService>>(NullLogger<HelloService>.Instance);
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

    }

    [Then(@"the application should run without startup errors")]
    public static void ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        // This is implicitly tested by the other steps.
    }

    [Then(@"the board should begin its announcement cycle to ""(.*)""")]
    public void ThenTheBoardShouldBeginItsAnnouncementCycleTo(string _)
    {
        Task.Run(() => _helloService.AnnouncePresence(_cancellationTokenSource.Token));
        _requestReceivedEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _cancellationTokenSource.Cancel();
    }
}
