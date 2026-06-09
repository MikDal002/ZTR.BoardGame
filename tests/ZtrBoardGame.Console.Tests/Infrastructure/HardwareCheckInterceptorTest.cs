using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(HardwareCheckInterceptor))]
public class HardwareCheckInterceptorTest
{
    enum Behavior
    {
        Throw,
        ReturnTrue,
        ReturnFalse
    }

    class ThrowingConfigurer(Behavior forCanConfigure = Behavior.Throw, Behavior forIsConfigurationNeeded = Behavior.Throw) : ISystemConfigurer
    {

        public bool IsConfigurationNeeded()
            => forIsConfigurationNeeded switch
            {
                Behavior.Throw => throw new NotImplementedException(),
                Behavior.ReturnTrue => true,
                Behavior.ReturnFalse => false,
                _ => throw new ArgumentOutOfRangeException(nameof(forIsConfigurationNeeded), forIsConfigurationNeeded,
                    null)
            };

        public bool CanConfigure()
            => forCanConfigure switch
            {
                Behavior.Throw => throw new NotImplementedException(),
                Behavior.ReturnTrue => true,
                Behavior.ReturnFalse => false,
                _ => throw new ArgumentOutOfRangeException(nameof(forCanConfigure), forCanConfigure,
                    null)
            };

        public void Configure()
            => throw new NotImplementedException();

        public string Name { get; }
    }

    class AnyOtherSettings : CommandSettings
    {
    }

    HardwareCheckInterceptor _hardwareCheckInterceptor = new(
        new TestConsole(),
        Options.Create(new HardwareConfigurationSettings()),
        []
    );

    [Test]
    public void HardwareCheckInterceptor_DoesNot_ThrowOnEmptyList()
    {
        // Arrange
        var cut = new HardwareCheckInterceptor(
            new TestConsole(),
            Options.Create(new HardwareConfigurationSettings()),
            []
        );

        // Act
        var action = () => cut.Intercept(null, null);

        // Assert
        action.Should().NotThrow();
    }

    [Test]
    public void HardwareCheckInterceptor_ProcessOnly_ForBoardRunSettings()
    {
        // Arrange
        var cut = new HardwareCheckInterceptor(
            new TestConsole(),
            Options.Create(new HardwareConfigurationSettings()),
            [new ThrowingConfigurer()]
        );

        // Act
        var action = () => cut.Intercept(null, new AnyOtherSettings());

        // Assert
        action.Should().NotThrow();
    }
}
