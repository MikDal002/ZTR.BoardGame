# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/SystemdConfigurer.cs:72

```csharp
        var workingDirectory = Path.GetDirectoryName(RealAppPath);

        var serviceContent = $@"
[Unit]
Description=Ztr Board Game Service
After=network.target multi-user.target

[Service]
Type=simple
User=root
WorkingDirectory={workingDirectory}
```

Code is used in the `InstallSystemdServiceAsync` method to create the systemd unit file content.

# What is wrong
The systemd service is configured to run as the `root` user. This is a significant security risk. Services should generally run with the least privileges necessary. Running the entire app as `root` can expose the system if there are any vulnerabilities in the application.

Furthermore, `WorkingDirectory={workingDirectory}` will resolve to the directory containing the AppImage, but running as `root` might cause permissions issues if the app creates files in this directory.

# Solution
Run the service as a dedicated user or a standard user instead of `root`. If root privileges are specifically needed for certain commands (like `uhubctl` or modifying system configuration), consider configuring `sudo` for those specific commands or running only those parts as root, rather than the entire application. If it really needs to be root, document clearly why. At least, make the user configurable.

# Assessment
4/6 - Running as root is an anti-pattern for security, but might be temporarily needed for hardware access if proper udev rules aren't set up. However, it's a code smell and should be addressed.
