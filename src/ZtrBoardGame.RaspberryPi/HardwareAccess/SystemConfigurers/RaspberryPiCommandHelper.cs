using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

internal static class RaspberryPiCommandHelper
{
    private static bool? _isRaspberryPi = false;
    public static bool IsRaspberryPiRoot()
    {
        _isRaspberryPi ??= IsRaspberryPiRootPriv();
        return _isRaspberryPi.Value;

        bool IsRaspberryPiRootPriv()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return false;
            }

            if (Environment.UserName != "root")
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

    private static bool _wasUpdateRun = false;
    private static object _syncContext = new();
    public static void RunUpdate()
    {
        lock (_syncContext)
        {
            if (_wasUpdateRun)
            {
                return;
            }

            _wasUpdateRun = true;
        }

        RunCommand("apt-get", "update", "Cannot run apt-get update");
    }
}
