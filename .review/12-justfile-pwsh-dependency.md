# Location and context
justfile:1

```justfile
default:
    #!pwsh
    just --list

[doc("Pack the application for Raspberry Pi (ARM64 Linux) using Velopack, skipping tests and formatting.")]
pack-rpi:
    #!pwsh
    .\build.cmd PackWithVelopack --SystemArchitecture arm64 --OperationSystem linux --skip E2ETests UnitTests CreateVersionLabel Format
```

Code is used in `justfile` to define the project's build targets.

# What is wrong
The `justfile` explicitly requires `pwsh` (PowerShell) for all of its targets by using the `#!pwsh` shebang. The targets themselves (`just --list` and `.\build.cmd ...`) do not actually require PowerShell and could be run in any shell. `.\build.cmd` is a batch script which won't run natively in `pwsh` on Linux anyway; it will try to invoke `cmd.exe` or fail. For cross-platform support with Nuke, you should be invoking `build.cmd` on Windows and `build.sh` on Linux.
Forcing `pwsh` here breaks the promise of `just` being a simple, cross-platform command runner.

# Solution
Remove the `#!pwsh` shebangs. For the `pack-rpi` target, use `just`'s built-in cross-platform capabilities or standard `sh` syntax. If you are using Nuke, the standard cross-platform way to invoke it is usually via `dotnet run --project build`.

```justfile
default:
    just --list

[doc("Pack the application for Raspberry Pi (ARM64 Linux) using Velopack, skipping tests and formatting.")]
pack-rpi:
    # Use build.sh on linux/mac, build.cmd on windows, or just dotnet run.
    ./build.sh PackWithVelopack --SystemArchitecture arm64 --OperationSystem linux --skip E2ETests UnitTests CreateVersionLabel Format
```

# Assessment
5/6 - Using PowerShell specifically for simple command execution across Windows and Linux adds unnecessary dependencies and complexity.
