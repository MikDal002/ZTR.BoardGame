using Microsoft.Extensions.Options;
using Moq;
using Spectre.Console.Testing;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
public class HardwareCheckInterceptorTests
{
    private Mock<IOptions<HardwareConfigurationSettings>> _mockConfig;
    private TestConsole _console;
    private HardwareConfigurationSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _mockConfig = new Mock<IOptions<HardwareConfigurationSettings>>();
        _console = new TestConsole();
        _settings = new HardwareConfigurationSettings { DoAutoConfig = false };
        _mockConfig.Setup(x => x.Value).Returns(_settings);
    }

    [TearDown]
    public void TearDown()
    {
        _console.Dispose();
    }

    [Test]
    public void Intercept_ShouldCallIsConfigurationNeeded_WhenAppropriateConfigurerFound()
    {
        // Arrange
        var mockConfigurer = new Mock<ISystemConfigurer>();
        mockConfigurer.Setup(x => x.CanConfigure()).Returns(true);
        mockConfigurer.Setup(x => x.IsConfigurationNeeded()).Returns(false);

        var interceptor = new HardwareCheckInterceptor(_console, _mockConfig.Object, new[] { mockConfigurer.Object });

        // Act
        interceptor.Intercept(null!, null!);

        // Assert
        mockConfigurer.Verify(x => x.IsConfigurationNeeded(), Times.Once);
    }

    [Test]
    public void Intercept_ShouldNotCallIsConfigurationNeeded_WhenNoAppropriateConfigurerFound()
    {
        // Arrange
        var mockConfigurer = new Mock<ISystemConfigurer>();
        mockConfigurer.Setup(x => x.CanConfigure()).Returns(false);

        var interceptor = new HardwareCheckInterceptor(_console, _mockConfig.Object, new[] { mockConfigurer.Object });

        // Act
        interceptor.Intercept(null!, null!);

        // Assert
        mockConfigurer.Verify(x => x.IsConfigurationNeeded(), Times.Never);
    }

    [Test]
    public void Intercept_ShouldCallConfigure_WhenConfigurationIsNeededAndUserConfirms()
    {
        // Arrange
        var mockConfigurer = new Mock<ISystemConfigurer>();
        mockConfigurer.Setup(x => x.CanConfigure()).Returns(true);
        mockConfigurer.Setup(x => x.IsConfigurationNeeded()).Returns(true);

        // Simulate user confirming (Spectre.Console.Testing)
        _console.Input.PushKey(ConsoleKey.Y);
        _console.Input.PushKey(ConsoleKey.Enter);

        var interceptor = new HardwareCheckInterceptor(_console, _mockConfig.Object, new[] { mockConfigurer.Object });

        // Act
        interceptor.Intercept(null!, null!);

        // Assert
        mockConfigurer.Verify(x => x.Configure(), Times.Once);
    }

    [Test]
    public void Intercept_ShouldNotCallConfigure_WhenConfigurationIsNeededButUserDeclines()
    {
        // Arrange
        var mockConfigurer = new Mock<ISystemConfigurer>();
        mockConfigurer.Setup(x => x.CanConfigure()).Returns(true);
        mockConfigurer.Setup(x => x.IsConfigurationNeeded()).Returns(true);

        // Simulate user declining
        _console.Input.PushKey(ConsoleKey.N);
        _console.Input.PushKey(ConsoleKey.Enter);

        var interceptor = new HardwareCheckInterceptor(_console, _mockConfig.Object, new[] { mockConfigurer.Object });

        // Act
        interceptor.Intercept(null!, null!);

        // Assert
        mockConfigurer.Verify(x => x.Configure(), Times.Never);
    }
}
