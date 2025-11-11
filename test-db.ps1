# Test Database Connection Script
Write-Host "Testing database connection..." -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/test-db" -Method Get -ContentType "application/json"
    
    Write-Host "✓ Connection successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "✗ Connection failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the application is running on http://localhost:5000" -ForegroundColor Yellow
}

