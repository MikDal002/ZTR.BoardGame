using FakeItEasy;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Infrastructure;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(HardwareCheckInterceptor))]
public class HardwareCheckInterceptorTest
{
    internal class ThrowingConfigurer() : ISystemConfigurer
    {
        public virtual bool IsConfigurationNeeded()
            => throw new NotImplementedException();

        public virtual bool CanConfigure()
            => throw new NotImplementedException();

        public virtual void Configure()
            => throw new NotImplementedException();

        public string Name { get; }
    }

    class AnyOtherSettings : CommandSettings
    {
    }

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
        A.CallTo(() => throwingConfigurer.CanConfigure()).MustHaveHappened();
        A.CallTo(() => throwingConfigurer.IsConfigurationNeeded()).MustHaveHappened();
    }

    [Test]
    public void FakeHardwareCheckInterceptor_ShouldThrow_WhenUserAccepts()
    {
        // Arrange
        var (cut, testConsole, throwingConfigurer) = Get(canConfigure: true, isConfigurationNeeded: true);

        // Act
        testConsole.Input.PushKey(ConsoleKey.Enter);
        var action = () => cut.Intercept(null, new BoardRunSettings());

        // Assert
        action.Should().NotThrow();
        testConsole.Output.Should().Contain("Exception");
        A.CallTo(() => throwingConfigurer.CanConfigure()).MustHaveHappened();
        A.CallTo(() => throwingConfigurer.IsConfigurationNeeded()).MustHaveHappened();
    }

    private static (HardwareCheckInterceptor cut, TestConsole testConsole, ISystemConfigurer throwingConfigurer) Get(bool? canConfigure = null, bool? isConfigurationNeeded = null)
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
