using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.HardwareAccess.SystemConfigurers;

[TestFixture]
[TestOf(typeof(RaspberryPiCommandHelper))]
public class RaspberryPiCommandHelperTest
{

    [Test]
    public async Task IsRaspberryPiRoot_ReturnsFalse_WhenNotOnRaspberryPi()
    {
        // Arrange & Act
        var isRaspberryPiRoot = await RaspberryPiCommandHelper.IsRaspberryPiRootAsync();

        // Assert
        isRaspberryPiRoot.Should().BeFalse();
    }
}
