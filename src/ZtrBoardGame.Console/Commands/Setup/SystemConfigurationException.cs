using System;

namespace ZtrBoardGame.Console.Commands.Setup;

public class SystemConfigurationException : Exception
{
    public SystemConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
