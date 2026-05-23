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
If `IsServiceConfiguredAsync` returns `false` (because the path changed), `ConfigureAsync` will be called. `ConfigureAsync` calls `InstallSystemdServiceAsync` which overwrites the file and calls `systemctl daemon-reload` and `systemctl enable`. However, it does not call `systemctl restart ztrboardgame.service`.
This means if the executable path is updated (e.g. app upgrade), the new configuration is written, but the *currently running* service is still the old one. The user would have to manually restart it or reboot the Pi for the new version to take effect.

# Solution
Add a command to restart the service after enabling it.

```csharp
        await RaspberryPiSystemInfo.RunCommandAsync("systemctl", "daemon-reload", "Cannot reload systemd daemon");
        await RaspberryPiSystemInfo.RunCommandAsync("systemctl", $"enable {ServiceName}", "Cannot enable service");
        await RaspberryPiSystemInfo.RunCommandAsync("systemctl", $"restart {ServiceName}", "Cannot restart service");
```

# Assessment
5/6 - Important for smooth upgrades. Without restarting, the user might be confused why the old version is still running despite the setup finishing successfully.
