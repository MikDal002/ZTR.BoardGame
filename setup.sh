#!/bin/bash

# Ensure PowerShell is installed
if ! command -v pwsh &> /dev/null
then
    echo "PowerShell (pwsh) is not installed. Please install it first."
    exit 1
fi

# Run the setup.ps1 script
pwsh -File ./setup.ps1
