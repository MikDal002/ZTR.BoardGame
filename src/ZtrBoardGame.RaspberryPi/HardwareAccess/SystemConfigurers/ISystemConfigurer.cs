namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public interface ISystemConfigurer
{
    bool CanConfigure();
    bool IsConfigurationNeeded();
    void Configure();
    string Name { get; }
}
