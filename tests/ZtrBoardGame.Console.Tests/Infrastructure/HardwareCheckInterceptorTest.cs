using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(SystemConfiguratorOrchestrator))]
public class SystemConfiguratorOrchestratorTest
{
    sealed class ThrowingConfigurer() : ISystemConfigurer
    {
        public Task<bool> IsConfigurationNeededAsync()
            => throw new NotImplementedException();

        public bool CanConfigure()
            => throw new NotImplementedException();

        public Task ConfigureAsync()
            => throw new NotImplementedException();

        public string Name { get; }
    }

    [Test]
    public async Task HardwareCheckInterceptor_DoesNot_ThrowOnEmptyList()
    {
        // Arrange
        var cut = new SystemConfiguratorOrchestrator([], NullLogger<SystemConfiguratorOrchestrator>.Instance);

        // Act
        var listAsync = await cut.GetSystemsWhichNeedsConfiguration().ToListAsync();

        // Assert
        listAsync.Should().BeEmpty();
    }

    [Test]
    public async Task HardwareCheckInterceptor_DoNotProcess_WhenCannotConfigure()
    {
        // Arrange
        var (cut, _) = Get(canConfigure: false);

        // Act
        var listAsync = await cut.GetSystemsWhichNeedsConfiguration().ToListAsync();

        // Assert
        listAsync.Should().BeEmpty();
    }

    [Test]
    public async Task HardwareCheckInterceptor_DoNotProcess_WhenConfigurationIsntNeeded()
    {
        var (cut, _) = Get(canConfigure: true, isConfigurationNeeded: false);

        // Act
        var listAsync = await cut.GetSystemsWhichNeedsConfiguration().ToListAsync();

        // Assert
        listAsync.Should().BeEmpty();
    }

    [Test]
    public async Task HardwareCheckInterceptor_ShouldPrint_Successfully()
    {
        // Arrange
        var (cut, _) = Get(canConfigure: true, isConfigurationNeeded: true, configure: true);

        // Act
        var listAsync = await cut.GetSystemsWhichNeedsConfiguration().ToListAsync();

        // Assert
        listAsync.Should().HaveCount(1);
    }

    [Test]
    public async Task FakeHardwareCheckInterceptor_ShouldThrow_WhenUserAccepts()
    {
        // Arrange
        var (cut, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        var action = async () => await cut.GetSystemsWhichNeedsConfiguration().ToListAsync();

        // Assert
        await action.Should().NotThrowAsync();
        A.CallTo(() => throwingConfigurer.CanConfigure()).MustHaveHappened();
        A.CallTo(() => throwingConfigurer.IsConfigurationNeededAsync()).MustHaveHappened();
    }

    private static (SystemConfiguratorOrchestrator cut, ISystemConfigurer throwingConfigurer) Get(
        bool? canConfigure = null, bool? isConfigurationNeeded = null, bool? configure = null)
    {
        var throwingConfigurer = A.Fake<ISystemConfigurer>(x => x.Wrapping(new ThrowingConfigurer()));

        if (canConfigure is not null)
        {
            A.CallTo(() => throwingConfigurer.CanConfigure())
                .Returns((bool)canConfigure);
        }

        if (isConfigurationNeeded is not null)
        {
            A.CallTo(() => throwingConfigurer.IsConfigurationNeededAsync())
                .Returns((bool)isConfigurationNeeded);
        }

        if (configure is not null)
        {
            A.CallTo(() => throwingConfigurer.ConfigureAsync())
                .DoesNothing();
        }

        var cut = new SystemConfiguratorOrchestrator(
            [throwingConfigurer],
            NullLogger<SystemConfiguratorOrchestrator>.Instance
        );

        return (cut, throwingConfigurer);
    }
}
