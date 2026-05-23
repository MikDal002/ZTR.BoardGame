Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/ConfigureSystemd.cs`, `IsServiceConfiguredAsync` method

What is wrong and why:
There is a fundamental logic error in checking if the service is configured. The application currently writes the service configuration with `ExecStart={RealAppPath} {appArguments}` where `appArguments` is hardcoded to `"board run"` inside `InstallSystemdServiceAsync`. However, `IsServiceConfiguredAsync` checks if the `serviceContent` contains `ExecStart={currentExePath}` (which resolves to just `RealAppPath` without arguments). This is highly brittle and could lead to false negatives if whitespaces differ or if we need to enforce exact matching. Furthermore, if `IsServiceConfiguredAsync` relies on finding just the path, it completely ignores whether the crucial `board run` arguments are actually present in the existing service file. This means if the file exists but has the wrong arguments, the application might incorrectly assume it's configured properly.

Proposed solution:
Build the exact `ExecStart` line in a single place (perhaps a helper method or property) and use that exact string for both checking the existing configuration and writing the new one. This ensures that the application verifies the complete command line, not just the executable path.
