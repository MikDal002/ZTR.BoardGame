using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Reqnroll;
using ZtrBoardGame.Console.Commands.PC;

namespace ZtrBoardGame.Console.Tests.StepDefinitions;

[Binding]
public class BoardConnectorStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IBoardStorage> _boardStorageMock = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<ILogger<BoardConnectorService>> _loggerMock = new();

    public BoardConnectorStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"PC is running")]
    public void GivenPcIsRunning()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_loggerMock.Object);
        services.AddSingleton(_boardStorageMock.Object);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<BoardConnectorService>();
        _scenarioContext.Set(services.BuildServiceProvider());
    }

    [Given(@"board is connected to the PC")]
    public void GivenBoardIsConnectedToThePc()
    {
        _boardStorageMock.Setup(x => x.GetAddresses()).Returns(new List<Uri> { new("http://localhost:5000") });
    }

    [When(@"board is not available")]
    public void WhenBoardIsNotAvailable()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));
    }

    [Then(@"PC will try to connect to the board")]
    public async Task ThenPcWillTryToConnectToTheBoard()
    {
        var serviceProvider = _scenarioContext.Get<ServiceProvider>();
        var service = serviceProvider.GetService<BoardConnectorService>();
        await service.ProcessBoardsAsync(CancellationToken.None);
    }

    [Then(@"it will fail")]
    public void ThenItWillFail()
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<HttpRequestException>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}
