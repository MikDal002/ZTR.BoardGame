using FakeItEasy;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Infrastructure;
using ZtrBoardGame.Console.Tests.TestHelpers;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(HardwareCheckInterceptor))]
public class HardwareCheckInterceptorTest
{
    sealed class ThrowingConfigurer() : ISystemConfigurer
    {
        public bool IsConfigurationNeeded()
            => throw new NotImplementedException();

        public bool CanConfigure()
            => throw new NotImplementedException();

        public void Configure()
            => throw new NotImplementedException();

        public string Name { get; }
    }

    sealed class AnyOtherSettings : CommandSettings;

    [Test]
    public void HardwareCheckInterceptor_DoesNot_ThrowOnEmptyList()
    {
        // Arrange
        var (cut, testConsole, _) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        var action = () => cut.Intercept(null, null);

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
    }

    [Test]
    public void HardwareCheckInterceptor_ProcessOnly_ForBoardRunSettings()
    // Arrange
    {
        var (cut, testConsole, _) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        var action = () => cut.Intercept(null, new AnyOtherSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
    }

    [Test]
    public void HardwareCheckInterceptor_DoNotProcess_WhenCannotConfigure()
    {
        // Arrange
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: false);

        // Act
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
        A.CallTo(() => throwingConfigurer.CanConfigure()).MustHaveHappened();
    }

    [Test]
    public void HardwareCheckInterceptor_DoNotProcess_WhenConfigurationIsntNeeded()
    {
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: false);

        // Act
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
        A.CallTo(() => throwingConfigurer.IsConfigurationNeeded()).MustHaveHappened();
    }

    [Test]
    public void FakeHardwareCheckInterceptor_ShouldNotThrow_WhenUserDeclines()
    {
        // Arrange
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        testConsole.Input.ForConfirm().PushAnswer(false);
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
        testConsole.Output.Should().Contain("skipped");
        A.CallTo(() => throwingConfigurer.IsConfigurationNeeded()).MustHaveHappened();
    }

    [Test]
    public void HardwareCheckInterceptor_ShouldPrint_Successfully()
    {
        // Arrange
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: true, configure: true);

        // Act
        testConsole.Input.ForConfirm().PushAnswer(true);
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().NotContain("Exception");
        testConsole.Output.Should().Contain("successfully");
        A.CallTo(() => throwingConfigurer.Configure()).MustHaveHappened();
    }

    [Test]
    public void FakeHardwareCheckInterceptor_ShouldThrow_WhenUserAccepts()
    {
        // Arrange
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        testConsole.Input.ForConfirm().PushAnswer(true);
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().Contain("Exception");
        A.CallTo(() => throwingConfigurer.CanConfigure()).MustHaveHappened();
        A.CallTo(() => throwingConfigurer.IsConfigurationNeeded()).MustHaveHappened();
    }

    private static (HardwareCheckInterceptor cut, TestConsole testConsole, ISystemConfigurer throwingConfigurer) Get(
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
            A.CallTo(() => throwingConfigurer.IsConfigurationNeeded())
                .Returns((bool)isConfigurationNeeded);
        }

        if (configure is not null)
        {
            A.CallTo(() => throwingConfigurer.Configure())
                .DoesNothing();
        }

        var testConsole = new TestConsole();
        testConsole.Profile.Capabilities.Interactive = true;

        var cut = new HardwareCheckInterceptor(
            testConsole,
            Options.Create(new HardwareConfigurationSettings()),
            [throwingConfigurer]
        );

        return (cut, testConsole, throwingConfigurer);
    }
}
