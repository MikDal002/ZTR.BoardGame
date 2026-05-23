# Location and context
src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/UsbPowerSystemConfigurer.cs:81

```csharp
        try
        {
            // Get current crontab, append new entry, and pipe back to crontab
            // If crontab is empty, we just set the entry
            var currentCrontab = string.Empty;
            try
            {
                currentCrontab = await RaspberryPiSystemInfo.RunCommandAsync("crontab", "-l", "Read crontab", redirectStandardOutput: true, redirectStandardError: true);
            }
            catch
            {
                // Ignore error if crontab doesn't exist yet
            }

            var newCrontab = string.IsNullOrEmpty(currentCrontab)
                ? CronEntry
                : $"{currentCrontab}\n{CronEntry}";
```

Code is used in `ConfigureCronAsync` method to add an entry to the user's crontab.

# What is wrong
This approach modifies the user's crontab blindly.
1. If the user already has this cron entry (perhaps configured slightly differently, e.g. with a different path or comments), it might append it again if the string doesn't perfectly match `CronEntry`, leading to duplicate entries running at reboot.
2. Appending blindly can lead to a messy crontab over time if `CronEntry` is changed slightly in the future.
3. System-wide tasks (like `uhubctl` which typically needs root or specific udev rules) are often better placed in system-wide cron (`/etc/cron.d/` or `/etc/crontab`) or as a `systemd` oneshot service, especially since systemd is already being utilized in the project.

# Solution
Instead of editing the user's crontab which is fragile, consider using systemd since it's already used for the main app. Create a simple `oneshot` systemd service that runs `uhubctl -a off` at startup.

Alternatively, if crontab must be used, add comments around the managed section to cleanly update or remove it later, e.g.,
`# BEGIN ZTR MANAGED`
`@reboot uhubctl -a off`
`# END ZTR MANAGED`
and use regex to update that specific block.

# Assessment
5/6 - Modifying user crontabs programmatically without boundaries is a frequent source of issues and hard-to-debug behaviors. Moving to systemd oneshot is much cleaner.
