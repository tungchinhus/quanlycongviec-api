# Script to start the application and open Swagger
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nStarting application on http://localhost:5000..." -ForegroundColor Green
Write-Host "Swagger will be available at: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the application`n" -ForegroundColor Yellow

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Start the application
dotnet run --launch-profile http

