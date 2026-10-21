using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Commands.Setup;

[TestFixture]
[TestOf(typeof(SetupCommand))]
public class SetupCommandTest
{
    private TestConsole _console;
    private TestableSetupCommand _setupCommand;
    private List<ISystemConfigurer> _configurers;

    private class TestableSetupCommand : SetupCommand
    {
        public TestableSetupCommand(IAnsiConsole console, ISystemConfiguratorOrchestrator orchestrator, ILogger<SetupCommand> logger)
            : base(console, orchestrator, logger)
        {
        }

        public Task<int> RunExecuteAsync(CommandContext context, SetupSettings settings, CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync(context, settings, cancellationToken);
        }
    }

    [SetUp]
    public void SetUp()
    {
        _console = new TestConsole();
        _configurers = new List<ISystemConfigurer>();
        var orchestrator = new SystemConfiguratorOrchestrator(_configurers, NullLogger<SystemConfiguratorOrchestrator>.Instance);
        _setupCommand = new TestableSetupCommand(_console, orchestrator, NullLogger<SetupCommand>.Instance);
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
        await _setupCommand.RunExecuteAsync(null, null);

        // Assert
        _console.Output.Should().Contain("Everything is already configured");
    }

    [Test]
    public async Task SetupCommand_WithEmptyListOfConfigurers_ReturnsConfiguredSuccessfully()
    {
        // Arrange
        _configurers.Add(new FakeSystemConfigurer());

        // Act
        await _setupCommand.RunExecuteAsync(null, null);

        // Assert
        _console.Output.Should().Contain("configured successfully");
    }
}
