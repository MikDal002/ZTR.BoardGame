# Location and context
src/ZtrBoardGame.Console/Program.cs:45

```csharp
            config.AddCommand<UpdateCommand>("version")
                .WithExample("version", "--update");

            config.AddCommand<SetupCommand>("setup");

            config.SetExceptionHandler((ex, _) =>
```

Code is used in `Program.cs` to register the new `SetupCommand`.

# What is wrong
The new `setup` command is added without a `.WithDescription(...)` or `.WithExample(...)`. Spectre.Console automatically generates help screens based on these configuration methods. Adding a command without a description means users running `your-app.exe --help` will see the `setup` command listed but won't have any idea what it actually does or how to use it without diving into the source code or running it blindly.

# Solution
Add a description and an example to the `SetupCommand` registration to improve CLI discoverability.

```csharp
            config.AddCommand<SetupCommand>("setup")
                .WithDescription("Configures the system for the ZTR Board Game (e.g., systemd, cron, I2C).")
                .WithExample("setup");
```

# Assessment
4/6 - CLI applications should be self-documenting. Omitting descriptions leads to a poor user experience.
