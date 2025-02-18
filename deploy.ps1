# Set working directory to the script's location
Set-Location -Path $PSScriptRoot

# Debug: Confirm the directory (optional)
Write-Host "Deploy Clipboard+ in: $(Get-Location)"

# Run the build command (path is now relative to the script's location)
dotnet run --project build/Build.csproj --target=Deploy

# Propagate the exit code
# exit $LASTEXITCODE