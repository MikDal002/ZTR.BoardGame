using Nuke.Common;
using Nuke.Common.Tooling;
using System;
using System.ComponentModel;

[TypeConverter(typeof(TypeConverter<SystemArchitecture>))]
public class SystemArchitecture : Enumeration
{
    public static SystemArchitecture Arm64 = new() { Value = "arm64" };
    public static SystemArchitecture X64 = new() { Value = "x64" };
    public static SystemArchitecture x86 = new() { Value = "x86" };

    public static implicit operator string(SystemArchitecture configuration)
    {
        return configuration.Value;
    }

    public static SystemArchitecture GetCurrentConfiguration()
    {
        if (EnvironmentInfo.IsArm64)
        {
            return Arm64;
        }
        else if (EnvironmentInfo.Is64Bit)
        {
            return X64;
        }
        else if (EnvironmentInfo.Is32Bit)
        {
            return x86;
        }
        else
        {
            throw new NotImplementedException("Unkown system!");
        }
    }
}
