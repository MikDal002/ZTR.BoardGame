
default:
    #!pwsh
    just --list

[doc("Pack the application for Raspberry Pi (ARM64 Linux) using Velopack, skipping tests and formatting.")]
pack-rpi:
    #!pwsh
    .\build.cmd PackWithVelopack --SystemArchitecture arm64 --OperationSystem linux --skip E2ETests UnitTests CreateVersionLabel Format
