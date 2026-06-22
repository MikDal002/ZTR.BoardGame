using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.Console.Infrastructure;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(HardwareCheckInterceptor))]
public class HardwareCheckInterceptorTest
{
    private TestConsole _console;
    private HardwareCheckInterceptor _interceptor;
    private List<ISystemConfigurer> _configurers;

    [SetUp]
    public void SetUp()
    {
        _console = new TestConsole();
        _configurers = new List<ISystemConfigurer>();
        var orchestrator = new SystemConfiguratorOrchestrator(_configurers, NullLogger<SystemConfiguratorOrchestrator>.Instance);
        _interceptor = new HardwareCheckInterceptor(_console, orchestrator);
    }

    [TearDown]
    public void TearDown()
    {
        _console?.Dispose();
    }

    [Test]
    public void Intercept_WhenNoSystemsNeedConfiguration_ShouldNotPrintWarning()
    {
        // Act
        _interceptor.Intercept(null!, null!);

        // Assert
        _console.Output.Should().BeEmpty();
    }

    [Test]
    public void Intercept_WhenSystemsNeedConfiguration_ShouldPrintWarning()
    {
        // Arrange
        _configurers.Add(new FakeSystemConfigurer { Name = "TestSystem" });

        // Act
        _interceptor.Intercept(null!, null!);

        // Assert
        _console.Output.Should().Contain("Below systems require configuration:");
        _console.Output.Should().Contain("- TestSystem");
        _console.Output.Should().Contain("Run Setup command to run configuration.");
    }

    private class FakeSystemConfigurer : ISystemConfigurer
    {
        public string Name { get; set; } = string.Empty;

        public Task<bool> IsConfigurationNeededAsync() => Task.FromResult(true);
        public Task<bool> CanConfigureAsync() => Task.FromResult(true);
        public Task ConfigureAsync() => Task.CompletedTask;
    }
}
