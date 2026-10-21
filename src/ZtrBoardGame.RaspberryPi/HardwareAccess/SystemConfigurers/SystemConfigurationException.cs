namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

public class SystemConfigurationException : Exception
{
    public SystemConfigurationException()
    {
    }

    public SystemConfigurationException(string message) : base(message)
    {
    }

    public SystemConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
