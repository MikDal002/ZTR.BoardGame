<#
.SYNOPSIS
    Cross-platform setup script for ZTR Board Game development environment.
#>

function Install-Just {
    Write-Output "--- Checking for 'just' ---" -ForegroundColor Cyan
    if (Get-Command just -ErrorAction SilentlyContinue) {
        Write-Output "'just' is already installed." -ForegroundColor Green
        return
    }

    if ($IsWindows) {
        Write-Output "Installing 'just' using winget..." -ForegroundColor Yellow
        winget install -e --id Casey.Just
    }
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
}

# --- Main Setup Execution ---

Write-Output "Starting environment setup..." -ForegroundColor Magenta

Install-Just

# Add more installation steps here:
# Install-Uhubctl
# Install-Dotnet

Write-Output "Setup finished!" -ForegroundColor Magenta
