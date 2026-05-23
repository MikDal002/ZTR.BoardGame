# Location and context
setup.ps1:19

```powershell
    elseif ($IsLinux) {
        Write-Output "Attempting to install 'just' on Linux..." -ForegroundColor Yellow
        if (Get-Command apt-get -ErrorAction SilentlyContinue) {
            sudo apt-get update
            sudo apt-get install -y just
        }
        elseif (Get-Command cargo -ErrorAction SilentlyContinue) {
            cargo install just
        }
        else {
            Write-Output "Error: No supported package manager found (apt/cargo)." -ForegroundColor Red
        }
    }
```

Code is used in the `setup.ps1` script to install the `just` command runner on Linux.

# What is wrong
The script blindly assumes `sudo` is available and required. This can fail in environments where the user is already root (e.g., Docker containers), environments where `sudo` is not installed, or environments where the user doesn't have `sudo` privileges and the script hangs waiting for a password prompt that might not be visible or expected in an automated CI/CD pipeline or silent install scenario.

# Solution
Check if the user is already root before prepending `sudo`. In PowerShell, you can check if the current user is root by checking the output of the `id -u` command. Alternatively, provide a mechanism to bypass sudo.

```powershell
        if (Get-Command apt-get -ErrorAction SilentlyContinue) {
            $SudoPrefix = if ((id -u) -eq 0) { "" } else { "sudo " }
            Invoke-Expression "$SudoPrefix apt-get update"
            Invoke-Expression "$SudoPrefix apt-get install -y just"
        }
```

# Assessment
4/6 - Scripts should be robust against running as root vs a normal user, especially in modern CI/CD and containerized environments.
