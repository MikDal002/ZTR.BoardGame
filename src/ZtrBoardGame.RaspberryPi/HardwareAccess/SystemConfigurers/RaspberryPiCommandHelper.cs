using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

internal static class RaspberryPiCommandHelper
{
    public static bool IsRaspberryPi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return false;
        }

        try
        {
            const string modelPath = "/proc/device-tree/model";
            if (File.Exists(modelPath))
            {
                var model = File.ReadAllText(modelPath);
                return model.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static string RunCommand(string command, string arguments, string errorMessage,
        bool redirectStandardOutput = false, bool redirectStandardError = false)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError
        };

        using var process = Process.Start(processStartInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Cannot start new process because of unknown error");
        }

        var output = string.Empty;

        if (redirectStandardOutput)
        {
            output = process.StandardOutput.ReadToEnd();
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{errorMessage} (Exit Code: {process.ExitCode})");
        }

        return output.Trim();
    }
}
