# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/SystemdConfigurer.cs:72

```csharp
        var serviceContent = $@"
[Unit]
Description=Ztr Board Game Service
After=network.target multi-user.target

[Service]
Type=simple
User=root
WorkingDirectory={workingDirectory}
ExecStart={RealAppPath} {appArguments}
Restart=always
RestartSec=5
Environment=DOTNET_ROOT=/usr/share/dotnet
StandardOutput=journal+console
StandardError=journal+console

[Install]
WantedBy=multi-user.target
";
```

Code is used in the `InstallSystemdServiceAsync` method to create the systemd unit file content.

# What is wrong
The systemd service unit file hardcodes `Environment=DOTNET_ROOT=/usr/share/dotnet`. While this is a common location for .NET on Linux, it might not be the correct location on all Raspberry Pi setups, especially if .NET was installed via a different method (e.g., install script placing it in `~/.dotnet/`, or snap).
Furthermore, if the application is bundled as a self-contained executable (or an AppImage which contains its own runtime), this environment variable might be unnecessary or even problematic if it conflicts with the bundled runtime. Given the path mentions an `.AppImage`, AppImages typically bundle their dependencies, making this variable likely redundant.

# Solution
Determine if the `DOTNET_ROOT` variable is strictly necessary. If the app is deployed as an AppImage or a self-contained binary, remove it. If a framework-dependent deployment is used, consider dynamically finding the `dotnet` installation path or making it configurable rather than hardcoding.

```csharp
// If AppImage or self-contained:
// Remove Environment=DOTNET_ROOT=/usr/share/dotnet from the template.
```

# Assessment
3/6 - Hardcoding environment paths can cause subtle deployment issues on non-standard setups. If it's an AppImage, it's likely unnecessary.
