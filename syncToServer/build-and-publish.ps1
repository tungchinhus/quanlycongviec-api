# Build and publish script for SyncToServer Windows Worker Service
# Creates a single executable file ready for deployment

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = ".\publish"
)

Write-Host "Building SyncToServer..." -ForegroundColor Green

# Build the project
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing as single executable..." -ForegroundColor Green

# Publish as single file
dotnet publish `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $OutputPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublish completed successfully!" -ForegroundColor Green
    Write-Host "Executable location: $OutputPath\syncToServer.exe" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Copy the entire 'publish' folder to the target machine" -ForegroundColor White
    Write-Host "2. Edit appsettings.json and configure:" -ForegroundColor White
    Write-Host "   - DestServer (network path)" -ForegroundColor White
    Write-Host "   - NetworkCredentials (username, password)" -ForegroundColor White
    Write-Host "   - IntervalMinutes" -ForegroundColor White
    Write-Host "3. Install as Windows Service (run as Administrator):" -ForegroundColor White
    Write-Host "   sc create SyncToServer binPath=`"$PWD\$OutputPath\syncToServer.exe`" start=auto" -ForegroundColor Cyan
    Write-Host "   sc start SyncToServer" -ForegroundColor Cyan
} else {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}
