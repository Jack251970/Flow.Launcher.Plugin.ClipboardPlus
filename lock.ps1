# Set working directory to the script's location
Set-Location -Path $PSScriptRoot

# Debug: Confirm the directory (optional)
Write-Host "Lock Clipboard+ in: $(Get-Location)"

# Restore the lock file
dotnet restore --use-lock-file