# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/UsbPowerSystemConfigurer.cs:100

```csharp
            // Using temporary file to avoid complex piping in RunCommandAsync
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            await File.WriteAllTextAsync(tempFile, newCrontab + "\n");

            try
            {
                await RaspberryPiSystemInfo.RunCommandAsync("crontab", tempFile, "Update crontab");
            }
            finally
            {
                File.Delete(tempFile);
            }
```

Code is used in `ConfigureCronAsync` to write the new crontab string to a temporary file and pass it to the `crontab` command.

# What is wrong
Using `Path.GetTempPath()` generally resolves to `/tmp/`. However, `/tmp/` is often a tmpfs and accessible to all users. Creating a temporary file with a random but predictable name and then passing it to a command that might execute with elevated privileges (if setup is run as root, which it likely is for installing uhubctl/systemd) can be a security risk (symlink attacks, though `GetRandomFileName` mitigates this somewhat).
More importantly, if the `File.WriteAllTextAsync` or `Path.Combine` throws an exception, the `tempFile` path string is evaluated, but if it throws *before* `File.WriteAllTextAsync` completes, the finally block won't even be hit, or the file might not exist. This is mostly safe here, but conceptually, the `tempFile` should be created in a safer manner or `crontab` should be fed via standard input.

# Solution
`crontab -` accepts input from stdin. It is much cleaner to extend `RaspberryPiSystemInfo.RunCommandAsync` to accept a `standardInput` string parameter and pipe the new crontab directly to `crontab -`, avoiding temporary files entirely.

```csharp
    public static async Task<string> RunCommandAsync(string command, string arguments, string errorMessage,
        bool redirectStandardOutput = false, bool redirectStandardError = false, string? standardInput = null)
    {
       // ... configure RedirectStandardInput = standardInput != null ...
       // ... process.StandardInput.WriteAsync(standardInput); process.StandardInput.Close(); ...
    }
```
Then call:
```csharp
await RaspberryPiSystemInfo.RunCommandAsync("crontab", "-", "Update crontab", standardInput: newCrontab + "\n");
```

# Assessment
4/6 - Avoiding temporary files for simple text piping reduces disk I/O and potential permission/security edge cases, and results in cleaner code.
