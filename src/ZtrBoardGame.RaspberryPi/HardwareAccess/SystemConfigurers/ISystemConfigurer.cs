namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public interface ISystemConfigurer
{
    bool CanConfigure();
    Task<bool> IsConfigurationNeededAsync();
    Task ConfigureAsync();
    string Name { get; }
}
