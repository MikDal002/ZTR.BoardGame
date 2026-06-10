using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "This runs real code")]
internal static class RaspberryPiCommandHelper
{
    private static bool? _isRaspberryPi = false;
    private static readonly Lock APT_SYNC_CONTEXT = new();
    private static readonly Lock RASPBERRYPI_CHECK_SYNC_CONTEXT = new();

    public static bool IsRaspberryPiRoot()
    {
        using var _ = RASPBERRYPI_CHECK_SYNC_CONTEXT.EnterScope();

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
        using var enterScope = APT_SYNC_CONTEXT.EnterScope();

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
            throw new InvalidOperationException($"{errorMessage} (Exit Code: {process.ExitCode}) with content {process.StandardError.ReadToEnd()}");
        }

        return output.Trim();
    }

    private static bool _wasUpdateRun = false;

    public static void RunUpdate()
    {
        using var enterScope = APT_SYNC_CONTEXT.EnterScope();

        if (_wasUpdateRun)
        {
            return;
        }

        RunCommand("apt-get", "update", "Cannot run apt-get update");

        _wasUpdateRun = true;
    }
}
