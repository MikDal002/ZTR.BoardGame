using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

[ExcludeFromCodeCoverage(Justification = "This runs real code")]
internal static class RaspberryPiCommandHelper
{
    private static bool? _isRaspberryPi = null;
    private static bool _wasUpdateRun = false;
    private static readonly SemaphoreSlim APT_SYNC_CONTEXT = new(1, 1);
    private static readonly SemaphoreSlim RASPBERRYPI_CHECK_SYNC_CONTEXT = new(1, 1);

    public static async Task<bool> IsRaspberryPiRootAsync()
    {
        try
        {
            await RASPBERRYPI_CHECK_SYNC_CONTEXT.WaitAsync();

            _isRaspberryPi ??= await IsRaspberryPiRootPrivAsymc();
            return _isRaspberryPi.Value;
        }
        finally
        {
            RASPBERRYPI_CHECK_SYNC_CONTEXT.Release();
        }

        async Task<bool> IsRaspberryPiRootPrivAsymc()
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
                    var model = await File.ReadAllTextAsync(modelPath);
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

    public static async Task<string> RunCommandAsync(string command, string arguments, string errorMessage,
        bool redirectStandardOutput = false)
    {
        try
        {
            await APT_SYNC_CONTEXT.WaitAsync();

            return await RunCommandInternalAsync(command, arguments, errorMessage, redirectStandardOutput);
        }
        finally
        {
            APT_SYNC_CONTEXT.Release();
        }
    }

    public static async Task RunUpdate()
    {
        try
        {
            await APT_SYNC_CONTEXT.WaitAsync();

            if (_wasUpdateRun)
            {
                return;
            }

            await RunCommandInternalAsync("apt-get", "update", "Cannot run apt-get update");

            _wasUpdateRun = true;
        }
        finally
        {
            APT_SYNC_CONTEXT.Release();
        }
    }

    static async Task<string> RunCommandInternalAsync(string command, string arguments, string errorMessage,
        bool redirectStandardOutput = false)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = true
        };

        using var process = Process.Start(processStartInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Cannot start new process because of unknown error");
        }

        var output = string.Empty;

        if (redirectStandardOutput)
        {
            output = await process.StandardOutput.ReadToEndAsync();
        }

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            return output.Trim();
        }

        var readToEndAsync = await process.StandardError.ReadToEndAsync();
        throw new InvalidOperationException(
            $"{errorMessage} (Exit Code: {process.ExitCode}) with content {readToEndAsync}");

    }
}
