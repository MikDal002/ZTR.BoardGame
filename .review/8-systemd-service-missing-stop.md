# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/SystemdConfigurer.cs:40

```csharp
        try
        {
            var serviceContent = await File.ReadAllTextAsync(ServicePath);
            if (!serviceContent.Contains($"ExecStart={currentExePath}"))
            {
                logger.LogWarning("Service file points to wrong path. Reconfiguration needed.");
                return false;
            }
        }
```

Code is used in `IsServiceConfiguredAsync` to verify if the existing service points to the correct executable path.

# What is wrong
If the system determines that the service needs to be reconfigured because the executable path has changed, it proceeds to overwrite the service file.
However, it does not stop the *currently running* service before overwriting the file and restarting. While `systemctl restart` (which I suggested adding in another comment) might handle this, it's safer to explicitly stop the old service before overwriting its unit file, or at least be aware that the old process continues running while the new unit file is written. The main issue is that overwriting an active unit file might lead to warnings from systemd about the unit file changing on disk without a daemon-reload.

# Solution
Add logic to stop the service before overwriting the unit file if the service is already running.

```csharp
    private async Task InstallSystemdServiceAsync()
    {
        logger.LogInformation("Installing systemd service for auto-start...");

        if (File.Exists(ServicePath))
        {
            try
            {
                await RaspberryPiSystemInfo.RunCommandAsync("systemctl", $"stop {ServiceName}", "Stopping existing service");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to stop existing service. It might not be running.");
            }
        }
        // ... proceed with creating file
```

# Assessment
3/6 - A minor improvement for robustness when upgrading the application path.
