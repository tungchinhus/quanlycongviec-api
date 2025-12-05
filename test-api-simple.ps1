# Simple script to test WorkItem API update
param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$WorkItemId = 66
)

Write-Host "Testing WorkItem API Update..." -ForegroundColor Cyan
Write-Host "WorkItem ID: $WorkItemId" -ForegroundColor Gray
Write-Host ""

# Check if app is running
try {
    $test = Invoke-WebRequest -Uri "$BaseUrl/api/work-items/test" -Method Get -UseBasicParsing -TimeoutSec 5
    Write-Host "App is running" -ForegroundColor Green
}
catch {
    Write-Host "App is not running! Please start it first." -ForegroundColor Red
    Write-Host "Run: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test data
$body = @{
    workType = "Core Design"
    startDate = "2025-11-26T17:00:00.000Z"
    expectedFinish = "2025-11-26T17:00:00.000Z"
    actualFinish = "2025-11-26T17:00:00.000Z"
    personConfirmation = $false
    notes = "Test update"
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "Sending PUT request..." -ForegroundColor Yellow
Write-Host "Body: $body" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/work-items/$WorkItemId" -Method Put -Headers $headers -Body $body -UseBasicParsing
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Cyan
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    Write-Host ""
    Write-Host "The InvalidCastException has been fixed!" -ForegroundColor Green
}
catch {
    Write-Host "FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "Error Body: $errorBody" -ForegroundColor Red
        }
        catch {
            Write-Host "Could not read error body" -ForegroundColor Yellow
        }
    }
    
    exit 1
}

