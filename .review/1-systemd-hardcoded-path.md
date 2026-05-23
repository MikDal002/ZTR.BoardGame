# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/SystemdConfigurer.cs:11

```csharp
    /// <summary>
    /// Unfortunately Environment.ProcessPath and AppContext.BaseDirectory returns path to the /tmp/.mound.../
    /// directory, which is changed on every run and is not the real path to the executable file. This is because of the
    /// way AppImage works.
    /// </summary>
    private const string RealAppPath = "/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage";
```

Code is used in the `SystemdConfigurer` class to define the executable path.

# What is wrong
The executable path is hardcoded to a specific user's home directory (`/home/mikolaj/...`) and a specific version/architecture of the AppImage. This will break on any other Raspberry Pi device, for any other user, or when a new version of the app is deployed. If the app is run from a different location, the systemd service will still point to this hardcoded path. Also, if this path doesn't exist, it will throw a `FileNotFoundException` during `InstallSystemdServiceAsync()`.

# Solution
Instead of hardcoding the path, we need a reliable way to get the AppImage path. In AppImages, the environment variables `APPIMAGE` or `OWD` are usually set, pointing to the original AppImage file. We can retrieve the environment variable `APPIMAGE` and fallback to `Environment.ProcessPath` if it's not set (e.g. not running as an AppImage).

```csharp
    private string GetRealAppPath()
    {
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
        return !string.IsNullOrEmpty(appImagePath) ? appImagePath : Environment.ProcessPath;
    }
```

# Assessment
6/6 - This is a critical bug. The current implementation only works on the author's specific machine and user account.
