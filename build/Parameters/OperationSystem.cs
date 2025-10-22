using System;
using System.ComponentModel;

[TypeConverter(typeof(TypeConverter<OperationSystem>))]
public class OperationSystem : Enumeration
{
    public static OperationSystem Linux = new() { Value = "linux" };
    public static OperationSystem Windows = new() { Value = "win" };
    public static OperationSystem Osx = new() { Value = "osx" };

    public static implicit operator string(OperationSystem configuration)
    {
        return configuration.Value;
    }

    public static OperationSystem GetCurrentConfiguration()
    {
        if (EnvironmentInfo.IsLinux)
        {
            return Linux;
        }
        else if (EnvironmentInfo.IsWin)
        {
            return Windows;
        }
        else if (EnvironmentInfo.IsOsx)
        {
            return Osx;
        }
        else
        {
            throw new NotImplementedException("Unkown system!");
        }
    }
}
