# Location and context
setup.sh:3

```bash
# Ensure PowerShell is installed
if ! command -v pwsh &> /dev/null
then
    echo "PowerShell (pwsh) is not installed. Please install it first."
    # code stops here
fi
```

Code is used in `setup.sh` to bootstrap the environment setup.

# What is wrong
The project forces developers on Linux to install PowerShell (`pwsh`) just to run a setup script that installs `just`. The `justfile` itself then uses `#!pwsh` as its default shell. This is an unnecessarily high barrier to entry for a Linux/Raspberry Pi project. Most Linux developers don't have PowerShell installed by default. If the goal is cross-platform scripts, `pwsh` makes sense, but forcing it just to bootstrap `just` defeats the purpose of a simple setup. The `setup.ps1` logic is simple enough that it could be replicated in standard `bash` within `setup.sh`.

# Solution
Implement the `just` installation logic directly in standard bash within `setup.sh` instead of requiring PowerShell. Or better yet, document the manual installation step for `just` and remove `setup.sh/ps1` entirely if it only installs one tool.

```bash
#!/bin/bash

# Simple setup script to install just
if ! command -v just &> /dev/null
then
    echo "Attempting to install 'just'..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get update && sudo apt-get install -y just
    elif command -v cargo &> /dev/null; then
        cargo install just
    else
        echo "Please install 'just' manually: https://github.com/casey/just"
        # code stops here
    fi
fi
echo "Setup complete."
```

# Assessment
5/6 - Forcing a heavy dependency like PowerShell on Linux users just to install a build runner is a poor developer experience.
