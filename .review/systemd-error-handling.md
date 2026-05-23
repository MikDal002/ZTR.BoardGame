Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/ConfigureSystemd.cs`, `IsServiceConfiguredAsync` method

What is wrong and why:
In the second `try/catch` block, you are calling `RaspberryPiSystemInfo.RunCommandAsync("systemctl", "is-enabled ...")` and catching an exception to return `false`. However, the `systemctl is-enabled` command exits with code `1` (which throws an exception in your current `RunCommandAsync` implementation) if the service is *disabled*. By catching this and returning `false`, you are successfully detecting that the service needs configuration. BUT, if an actual error occurs (e.g., `systemctl` is not installed, or you don't have permissions), it will *also* catch the exception and return `false`, proceeding to attempt configuration. This is masking potential system errors by treating them as "service not enabled yet".

Proposed solution:
Fix `RunCommandAsync` to not throw on non-zero exit codes. Then, explicitly check if the exit code is `0` (enabled) or `1` (disabled). If the exit code is something else, throw an actual exception or log a critical error because the system state is unreadable.
