Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/ConfigureSystemd.cs`, Line 9

What is wrong and why:
The `RealAppPath` constant is hardcoded to `"/home/mikolaj/ZtrBoardGame.Console-linux-arm64-alpha.AppImage"`. This implies the service will literally fail to start for any user whose name is not `mikolaj` or if they named the binary differently. Hardcoding an absolute path that is specific to one developer's machine in source code guarantees this code is broken out of the box for anyone else or on any production device. Furthermore, running this service as `User=root` in the generated systemd unit file is a massive security risk, especially when executing a binary from a user's home directory.

Proposed solution:
Dynamically resolve the application path. You can use `Environment.ProcessPath` or `AppContext.BaseDirectory` to find the exact location of the currently executing application, rather than hardcoding it. Also, the service should run as an unprivileged service account or the current user (e.g., using `Environment.UserName`) instead of `root`, unless root access is absolutely required for hardware control (in which case, document why).
