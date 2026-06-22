namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

public interface ISystemConfigurer
{
    Task<bool> IsConfigurationNeededAsync();
    Task<bool> CanConfigureAsync();
    Task ConfigureAsync();
    string Name { get; }
}
