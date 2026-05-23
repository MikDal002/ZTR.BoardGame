Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/UsbPowerSystemConfigurer.cs`

What is wrong and why:
The application is invoking `apt-get install -y uhubctl` directly during its runtime. This is a severe architectural violation. Application code should not act as a system provisioner. Running `apt-get` requires root privileges, meaning the entire application must be run as root to configure itself, which violates the principle of least privilege. Furthermore, `apt-get` can fail for numerous environment-specific reasons (locks held by unattended-upgrades, missing network, prompt blocks) which the app cannot handle gracefully.

Proposed solution:
Remove package installation logic from the application code. System dependencies should be resolved prior to application execution, either via a dedicated setup script (e.g., `install-deps.sh`), a Debian package, or a configuration management tool. The C# application should only *verify* that dependencies exist and fail fast with a clear, actionable error message instructing the user on what to install.
