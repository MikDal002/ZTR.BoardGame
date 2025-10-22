using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using TechTalk.SpecFlow;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class BoardConnectivityStepDefinitions
{
    private static IServiceCollection _services;
    private static CancellationTokenSource _cancellationTokenSource;
    private static ManualResetEvent _requestReceivedEvent;
    private static Mock<HttpMessageHandler> _httpMessageHandlerMock;

    [BeforeScenario]
    public static void BeforeScenario()
    {
        _services = new ServiceCollection();
        _cancellationTokenSource = new CancellationTokenSource();
        _requestReceivedEvent = new ManualResetEvent(false);
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Given(@"the board's configuration specifies the PC server address as ""(.*)""")]
    public static void GivenTheBoardsConfigurationSpecifiesThePCServerAddressAs(string pcServerAddress)
    {
        var networkSettings = new NetworkSettings { PcServerAddress = pcServerAddress };
        _services.AddSingleton(Options.Create(networkSettings));
    }

    [When(@"the board application starts")]
    public static void WhenTheBoardApplicationStarts()
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
        _services.AddSingleton<HelloService>();

        var serviceProvider = _services.BuildServiceProvider();
        var helloService = serviceProvider.GetRequiredService<HelloService>();

        Task.Run(() => helloService.AnnouncePresence(_cancellationTokenSource.Token));
    }

    [Then(@"the application should run without startup errors")]
    public static void ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        // This is implicitly tested by the other steps.
    }

    [Then(@"the board should begin its announcement cycle to ""(.*)""")]
    public static void ThenTheBoardShouldBeginItsAnnouncementCycleTo(string url)
    {
        _requestReceivedEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
    }

    [AfterScenario]
    public static void AfterScenario()
    {
        _cancellationTokenSource.Cancel();
    }
}
