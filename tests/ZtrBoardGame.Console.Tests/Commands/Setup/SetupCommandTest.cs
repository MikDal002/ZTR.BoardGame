using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Commands.Setup;

[TestFixture]
[TestOf(typeof(SetupCommand))]
public class SetupCommandTest
{
    private TestConsole _console;
    private SetupCommand _setupCommand;
    private List<ISystemConfigurer> _configurers;

    [SetUp]
    public void SetUp()
    {
        _console = new TestConsole();
        _configurers = new List<ISystemConfigurer>();
        var orchestrator = new SystemConfiguratorOrchestrator(_configurers, NullLogger<SystemConfiguratorOrchestrator>.Instance);
        _setupCommand = new SetupCommand(_console, orchestrator, NullLogger<SetupCommand>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _console?.Dispose();
    }

    private class FakeSystemConfigurer : ISystemConfigurer
    {
        public string Name { get; set; } = string.Empty;

        public Task<bool> IsConfigurationNeededAsync() => Task.FromResult(true);
        public Task<bool> CanConfigureAsync() => Task.FromResult(true);
        public Task ConfigureAsync() => Task.CompletedTask;
    }

    [Test]
    public async Task SetupCommand_WithEmptyListOfConfigurers_ReturnsEverythingIsDone()
    {
        // Act
        await _setupCommand.ExecuteAsync(null, null);

        // Assert
        _console.Output.Should().Contain("Everything is already configured");
    }

    [Test]
    public async Task SetupCommand_WithEmptyListOfConfigurers_ReturnsConfiguredSuccessfully()
    {
        // Arrange
        _configurers.Add(new FakeSystemConfigurer());

        // Act
        await _setupCommand.ExecuteAsync(null, null);

        // Arrange
        _console.Output.Should().Contain("configured successfully");
    }
}
