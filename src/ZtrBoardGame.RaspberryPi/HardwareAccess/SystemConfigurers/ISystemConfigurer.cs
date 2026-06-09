namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public interface ISystemConfigurer
{
    bool IsConfigurationNeeded();
    bool CanConfigure();
    void Configure();
    string Name { get; }
}
