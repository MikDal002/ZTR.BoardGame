# Location and context
src/ZtrBoardGame.Console/Commands/Setup/SetupCommand.cs:18

```csharp
public class SetupSettings : CommandSettings
{
    [CommandOption("--limit")]
    [Description("Specifies the limit for the operation.")]
    public int? Limit { get; set; } = 4;

    [CommandOption("--directory")]
    [Description("Specifies the target directory.")]
    public DirectoryInfo? Directory { get; set; }
```

Code is used in `SetupCommand.cs` to define CLI arguments for the `setup` command.

# What is wrong
The `SetupSettings` class defines `--limit` and `--directory` options, but based on the system configuration implementations (`SystemdConfigurer`, `UsbPowerSystemConfigurer`), these options are entirely irrelevant to what the setup command actually does (configuring system services and cron). These look like copy-pasted boilerplate settings from another command that were never cleaned up. Exposing irrelevant options confuses users.

# Solution
Remove the unused `Limit` and `Directory` properties from `SetupSettings` entirely. If the setup command requires specific arguments (like the `appArguments` for systemd mentioned in another review), add those instead.

```csharp
public class SetupSettings : CommandSettings
{
    // Add relevant options here, e.g., --force, --skip-systemd, etc.
}
```

# Assessment
4/6 - Leaving boilerplate code in CLI options clutters the help menu and causes confusion.
