# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/SystemdConfigurer.cs:72

```csharp
        var appArguments = $"board run";

        // ...

        var serviceContent = $@"
[Unit]
Description=Ztr Board Game Service
After=network.target multi-user.target

[Service]
Type=simple
User=root
WorkingDirectory={workingDirectory}
ExecStart={RealAppPath} {appArguments}
```

Code is used in the `InstallSystemdServiceAsync` method to create the systemd unit file content.

# What is wrong
The arguments `board run` are hardcoded here. What if the `SystemdConfigurer` is called from a different branch of the application or with different desired startup arguments in the future? Hardcoding specific console application arguments deep inside a hardware configuration class couples the configuration mechanism directly to the specific CLI layout.

# Solution
Pass the required `appArguments` as a configuration option or parameter to the `SystemdConfigurer` rather than hardcoding it, or retrieve the original startup arguments using `Environment.GetCommandLineArgs()`.

```csharp
        // In ConfigureAsync or via constructor injection:
        var appArguments = _configuration.GetValue<string>("Systemd:StartupArguments") ?? "board run";
```

# Assessment
3/6 - A minor architectural issue (tight coupling) that could become annoying if the application's CLI structure changes.
