# Script to stop running app and rebuild
Write-Host "Stopping running application..." -ForegroundColor Yellow

# Kill process using port 5000
Write-Host "Checking for processes using port 5000..." -ForegroundColor Cyan
$port5000 = netstat -ano | findstr :5000
if ($port5000) {
    $pids = $port5000 | ForEach-Object {
        if ($_ -match '\s+(\d+)\s*$') {
            $matches[1]
        }
    } | Select-Object -Unique
    
    foreach ($pid in $pids) {
        if ($pid -and $pid -ne "0") {
            Write-Host "Killing process $pid using port 5000..." -ForegroundColor Yellow
            taskkill /F /PID $pid 2>$null
        }
    }
    Start-Sleep -Seconds 1
}

# Find and stop the running process
$processes = Get-Process -Name "quanlyfilesBE" -ErrorAction SilentlyContinue
if ($processes) {
    foreach ($process in $processes) {
        Write-Host "Stopping process: $($process.Id)" -ForegroundColor Cyan
        Stop-Process -Id $process.Id -Force
    }
    Start-Sleep -Seconds 2
}

# Also check for dotnet processes that might be running the app
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like "*quanlyfilesBE*" -or $_.CommandLine -like "*quanlyfilesBE*"
}
if ($dotnetProcesses) {
    Write-Host "Found dotnet processes, attempting to stop..." -ForegroundColor Cyan
    foreach ($process in $dotnetProcesses) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        } catch {}
    }
    Start-Sleep -Seconds 2
}

Write-Host "Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Build successful!" -ForegroundColor Green
    Write-Host "You can now run the application with: dotnet run" -ForegroundColor Cyan
} else {
    Write-Host "`n✗ Build failed!" -ForegroundColor Red
}

