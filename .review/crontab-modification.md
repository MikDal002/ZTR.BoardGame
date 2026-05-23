Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/UsbPowerSystemConfigurer.cs`

What is wrong and why:
The application reads the user's crontab, appends a raw `@reboot uhubctl -a off` string, writes it to a temporary file, and pipes it back. This is incredibly fragile. If the exact string `@reboot uhubctl -a off` changes slightly (e.g., extra spaces), `output.Contains(CronEntry)` will fail, leading to duplicate entries being added indefinitely. Modifying crontab programmatically without a proper parser is risky and can corrupt user-defined scheduled tasks.

Proposed solution:
Do not touch the user's crontab. Instead, create a simple systemd `oneshot` service unit that runs `uhubctl -a off` at boot and enable it. Systemd is idempotent, easier to query for status, and far less error-prone than raw text manipulation of a user's crontab.
