# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/UsbPowerSystemConfigurer.cs:58

```csharp
    private static async Task<bool> IsCronConfiguredAsync()
    {
        try
        {
            // crontab -l returns 1 if no crontab for user, which throws exception in RunCommandAsync
            var output = await RaspberryPiSystemInfo.RunCommandAsync("crontab", "-l", "Read crontab", redirectStandardOutput: true, redirectStandardError: true);
            return output.Contains(CronEntry);
        }
        catch
        {
            return false;
        }
    }
```

Code is used to check if the USB power off cron entry exists.

# What is wrong
The blanket `catch` block catches *all* exceptions, not just the one resulting from `crontab -l` exiting with code 1 (no crontab). If `RunCommandAsync` throws for any other reason (e.g. `crontab` executable not found, out of memory, unexpected syntax error), it will be silently swallowed, and the method will return `false`, causing the configuration to unnecessarily run again.
Additionally, catching all exceptions is a bad practice as it hides potential bugs.

# Solution
Catch only the specific exception that indicates "no crontab for user". If `RaspberryPiSystemInfo.RunCommandAsync` threw a specific exception with the exit code (e.g., as suggested in another review), we could check for exit code 1. Alternatively, catch `InvalidOperationException` and check the message.

```csharp
        catch (InvalidOperationException ex) when (ex.Message.Contains("Exit Code: 1"))
        {
            return false;
        }
```

# Assessment
4/6 - Catching general exceptions is a bad practice. Being explicit about expected failures prevents hidden bugs.
