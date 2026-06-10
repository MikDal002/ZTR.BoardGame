using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Tests.HardwareAccess.SystemConfigurers;

[TestFixture]
[TestOf(typeof(RaspberryPiCommandHelper))]
public class RaspberryPiCommandHelperTest
{

    [Test]
    public void IsRaspberryPiRoot_ReturnsFalse_WhenNotOnRaspberryPi()
    {
        // Arrange & Act
        var isRaspberryPiRoot = RaspberryPiCommandHelper.IsRaspberryPiRoot();

        // Assert
        isRaspberryPiRoot.Should().BeFalse();
    }
}
