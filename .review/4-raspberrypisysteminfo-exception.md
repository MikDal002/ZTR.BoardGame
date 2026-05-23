# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/RaspberryPiSystemInfo.cs:48

```csharp
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

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{errorMessage} (Exit Code: {process.ExitCode})");
        }
```

Code is used in `RunCommandAsync` to execute shell commands.

# What is wrong
Throwing `InvalidOperationException` when a command returns a non-zero exit code hides the actual error. When `redirectStandardError` is true (which it is for `crontab -l` check in `UsbPowerSystemConfigurer`), the error output is completely ignored and lost. This makes debugging why a command failed impossible because the stderr is not logged or included in the exception message.

# Solution
Read `StandardError` if `redirectStandardError` is true, and include it in the exception message. Also, consider creating a specific exception type (e.g. `CommandExecutionException`) instead of reusing `InvalidOperationException`.

```csharp
        var errorOutput = string.Empty;
        if (redirectStandardError)
        {
            errorOutput = await process.StandardError.ReadToEndAsync();
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var msg = $"{errorMessage} (Exit Code: {process.ExitCode}).";
            if (!string.IsNullOrWhiteSpace(errorOutput)) msg += $" Error: {errorOutput.Trim()}";
            throw new InvalidOperationException(msg);
        }
```

# Assessment
6/6 - Silent failures are a developer's nightmare. Capturing stderr is crucial for diagnosing failing shell commands.
