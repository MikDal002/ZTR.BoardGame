using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll;
using ZtrBoardGame.Console.Controllers;
using ZtrBoardGame.Console.Tests.Infrastructure;
using Moq.Protected;

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class PcConnectivityStepDefinitions
{
    private CustomWebApplicationFactory<HelloController> _webApplicationFactory;
    private HttpClient _boardHttpClient;
    private Mock<ILogger<HelloController>> _mockLogger;
    private HttpResponseMessage _response;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _mockLogger = new Mock<ILogger<HelloController>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _webApplicationFactory = new CustomWebApplicationFactory<HelloController>();
        _webApplicationFactory.ConfigureTestServices(services =>
        {
            services.AddSingleton(_mockLogger.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_mockHttpMessageHandler.Object));
            services.AddSingleton(mockHttpClientFactory.Object);
        });
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _webApplicationFactory.Dispose();
        _boardHttpClient?.Dispose();
    }

    [Given(@"a board is configured with the PC server address")]
    public void GivenABoardIsConfiguredWithThePCServerAddress()
    {
        // This step is implicitly handled by the test setup
    }

    [Given(@"the PC server is running")]
    public void GivenThePCServerIsRunning()
    {
        _boardHttpClient = _webApplicationFactory.CreateClient();
    }

    [When(@"the board sends a ""hello"" request to the PC from IP ""(.*)""")]
    public async Task WhenTheBoardSendsAHelloRequestToThePCFromIP(string ipAddress)
    {
        _boardHttpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
        _response = await _boardHttpClient.PostAsync("/api/hello", null);
    }

    [When(@"the PC receives the request")]
    public void WhenThePCReceivesTheRequest()
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the PC's console log should contain an INFO message like ""(.*)""")]
    public void ThenThePCsConsoleLogShouldContainAnINFOMessageLike(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                It.IsAny<System.Exception>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception, string>>()),
            Times.Once);
    }

    [Then(@"the PC should immediately send a ""hello"" request back to ""(.*)""")]
    public void ThenThePCShouldImmediatelySendAHelloRequestBackTo(string url)
    {
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post
                && req.RequestUri == new System.Uri(url)
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
