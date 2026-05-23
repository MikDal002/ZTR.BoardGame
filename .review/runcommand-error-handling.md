Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/RaspberryPiSystemInfo.cs`, `RunCommandAsync` method

What is wrong and why:
When `process.ExitCode != 0`, the method throws an `InvalidOperationException` with a custom `errorMessage` and the exit code. We do not see how `RaspberryPiSystemInfo.RunCommandAsync` is fully implemented in this diff, but based on the usage and the exception thrown, it seems to just throw an exception on non-zero exit code without capturing the actual error output (`stderr`). A message like "Cannot install uhubctl (Exit Code: 100)" is useless compared to the actual error. Also, using exceptions for control flow in `IsUhubctlInstalledAsync` and `IsCronConfiguredAsync` is an anti-pattern. They catch a general exception and return `false`, which hides potential bugs or unexpected issues.

Proposed solution:
1. Ensure `RunCommandAsync` captures `StandardError` and includes it in the exception message if the process fails.
2. In `IsUhubctlInstalledAsync` and `IsCronConfiguredAsync`, check the exit code directly instead of throwing and catching an exception if possible, or at least be more specific about the expected exception type. Returning `false` on *any* exception is dangerous.
