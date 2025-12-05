# Script to test WorkItem API update with PersonConfirmation
# Tests the fix for InvalidCastException

param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$WorkItemId = 0,
    [string]$Token = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test WorkItem API Update" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if app is running
Write-Host "Checking if application is running..." -ForegroundColor Yellow
try {
    $testResponse = Invoke-WebRequest -Uri "$BaseUrl/api/work-items/test" -Method Get -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ Application is running" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ Application is not running!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start the application first:" -ForegroundColor Yellow
    Write-Host "  dotnet run" -ForegroundColor Cyan
    Write-Host "  or" -ForegroundColor Gray
    Write-Host "  .\rebuild-and-run.ps1" -ForegroundColor Cyan
    exit 1
}

# Get work item ID if not provided
if ($WorkItemId -eq 0) {
    Write-Host "Getting work item ID from database..." -ForegroundColor Yellow
    
    # Read connection string
    $appsettingsPath = Join-Path $PSScriptRoot "appsettings.json"
    if (Test-Path $appsettingsPath) {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        $connectionString = $appsettings.ConnectionStrings.DefaultConnection
        
        # Parse connection string
        $server = ""
        $database = ""
        $userId = ""
        $password = ""
        
        if ($connectionString -match 'Server=([^;]+)') {
            $server = $matches[1]
        }
        if ($connectionString -match 'Database=([^;]+)') {
            $database = $matches[1]
        }
        if ($connectionString -match 'User Id=([^;]+)') {
            $userId = $matches[1]
        }
        if ($connectionString -match 'Password=([^;]+)') {
            $password = $matches[1]
        }
        
        # Get first work item ID
        try {
            $sqlModule = Get-Module -ListAvailable -Name SqlServer
            if (-not $sqlModule) {
                Import-Module SqlServer -ErrorAction SilentlyContinue
            }
            
            $query = "SELECT TOP 1 WorkItemID FROM WorkItem ORDER BY WorkItemID"
            $result = Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $userId -Password $password -Query $query -TrustServerCertificate -ErrorAction SilentlyContinue
            
            if ($result) {
                $WorkItemId = $result.WorkItemID
                Write-Host "Found work item ID: $WorkItemId" -ForegroundColor Green
            }
            else {
                Write-Host "No work items found in database. Please create one first." -ForegroundColor Yellow
                exit 1
            }
        }
        catch {
            Write-Host "Could not get work item ID from database. Using default ID 66." -ForegroundColor Yellow
            $WorkItemId = 66
        }
    }
    else {
        Write-Host "appsettings.json not found. Using default ID 66." -ForegroundColor Yellow
        $WorkItemId = 66
    }
}

Write-Host ""
Write-Host "Testing WorkItem Update API" -ForegroundColor Cyan
Write-Host "  WorkItem ID: $WorkItemId" -ForegroundColor Gray
Write-Host "  Endpoint: $BaseUrl/api/work-items/$WorkItemId" -ForegroundColor Gray
Write-Host ""

# Prepare test data with personConfirmation = false (the problematic case)
$testData = @{
    workType = "Core Design"
    startDate = "2025-11-26T17:00:00.000Z"
    expectedFinish = "2025-11-26T17:00:00.000Z"
    actualFinish = "2025-11-26T17:00:00.000Z"
    personConfirmation = $false  # This was causing InvalidCastException
    notes = "Test update with personConfirmation = false"
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Yellow
Write-Host $testData -ForegroundColor Gray
Write-Host ""

# Prepare headers
$headers = @{
    "Content-Type" = "application/json"
}

if (-not [string]::IsNullOrEmpty($Token)) {
    $headers["Authorization"] = "Bearer $Token"
    Write-Host "Using provided JWT token" -ForegroundColor Green
}
else {
    Write-Host "WARNING: No JWT token provided. Request may fail with 401 Unauthorized." -ForegroundColor Yellow
    Write-Host "If you get 401, you need to login first and get a token." -ForegroundColor Yellow
    Write-Host ""
}

# Test 1: GET work item (to see current state)
Write-Host "Test 1: GET work item (current state)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray
try {
    $getResponse = Invoke-WebRequest -Uri "$BaseUrl/api/work-items/$WorkItemId" -Method Get -Headers $headers -UseBasicParsing -ErrorAction Stop
    $getData = $getResponse.Content | ConvertFrom-Json
    Write-Host "✓ GET successful" -ForegroundColor Green
    Write-Host "Current PersonConfirmation: $($getData.personConfirmation)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✗ GET failed: 401 Unauthorized - Need JWT token" -ForegroundColor Red
        Write-Host "Please login first and provide token with -Token parameter" -ForegroundColor Yellow
    }
    elseif ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✗ GET failed: 404 Not Found - Work item ID $WorkItemId does not exist" -ForegroundColor Red
    }
    else {
        Write-Host "✗ GET failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 2: PUT update work item (the main test)
Write-Host "Test 2: PUT update work item (with personConfirmation = false)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "This is the test that was failing with InvalidCastException..." -ForegroundColor Yellow
Write-Host ""

try {
    $putResponse = Invoke-WebRequest -Uri "$BaseUrl/api/work-items/$WorkItemId" -Method Put -Headers $headers -Body $testData -UseBasicParsing -ErrorAction Stop
    
    Write-Host "✓ PUT successful!" -ForegroundColor Green
    Write-Host "Status Code: $($putResponse.StatusCode)" -ForegroundColor Gray
    Write-Host ""
    
    $responseData = $putResponse.Content | ConvertFrom-Json
    Write-Host "Response Data:" -ForegroundColor Green
    $responseData | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
    Write-Host ""
    
    # Verify PersonConfirmation was saved correctly
    if ($responseData.personConfirmation -eq $false) {
        Write-Host "✓ PersonConfirmation = false was saved correctly!" -ForegroundColor Green
    }
    elseif ($null -eq $responseData.personConfirmation) {
        Write-Host "? PersonConfirmation is null" -ForegroundColor Yellow
    }
    else {
        Write-Host "? PersonConfirmation value: $($responseData.personConfirmation)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "SUCCESS: API test passed!" -ForegroundColor Green
    Write-Host "The InvalidCastException has been fixed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $null
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
    }
    
    Write-Host "✗ PUT failed!" -ForegroundColor Red
    if ($statusCode) {
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
    }
    Write-Host ""
    
    # Try to get error details from response
    try {
        if ($_.Exception.Response) {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            $errorJson = $errorBody | ConvertFrom-Json
            
            Write-Host "Error Details:" -ForegroundColor Red
            if ($errorJson.error) {
                Write-Host "  Error: $($errorJson.error)" -ForegroundColor Red
            }
            if ($errorJson.message) {
                Write-Host "  Message: $($errorJson.message)" -ForegroundColor Red
            }
            if ($errorJson.details) {
                Write-Host "  Details: $($errorJson.details)" -ForegroundColor Red
                
                # Check if it's still the InvalidCastException
                if ($errorJson.details -like "*InvalidCastException*" -or $errorJson.details -like "*Unable to cast*") {
                    Write-Host ""
                    Write-Host "========================================" -ForegroundColor Red
                    Write-Host "ERROR: InvalidCastException still occurs!" -ForegroundColor Red
                    Write-Host "The database migration may not have been applied." -ForegroundColor Yellow
                    Write-Host "Please run: .\fix-personconfirmation-migration.ps1" -ForegroundColor Cyan
                    Write-Host "========================================" -ForegroundColor Red
                }
            }
        }
        else {
            Write-Host "  Error: $errorMessage" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  Error: $errorMessage" -ForegroundColor Red
    }
    
    Write-Host ""
    
    if ($statusCode -eq 401) {
        Write-Host "Note: 401 Unauthorized - You need to provide a JWT token" -ForegroundColor Yellow
        Write-Host "Usage: .\test-workitem-api.ps1 -Token 'your_jwt_token'" -ForegroundColor Cyan
    }
    elseif ($statusCode -eq 404) {
        Write-Host "Note: 404 Not Found - Work item ID $WorkItemId does not exist" -ForegroundColor Yellow
        Write-Host "Usage: .\test-workitem-api.ps1 -WorkItemId <existing_id>" -ForegroundColor Cyan
    }
    elseif ($statusCode -eq 500) {
        Write-Host "Note: 500 Internal Server Error - Check application logs" -ForegroundColor Yellow
    }
    
    exit 1
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Green

