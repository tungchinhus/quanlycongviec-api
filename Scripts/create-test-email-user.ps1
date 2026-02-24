# Script tạo user test dùng để kiểm tra chức năng email
# User: tungchinhus@gmail.com (User Test Email)
# Chạy khi backend đang chạy (vd: http://localhost:5000)

param(
    [string]$ApiBaseUrl = $env:QUANLYFILES_API_URL,
    [string]$Password = "TestEmail123!"
)

if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    $ApiBaseUrl = "http://localhost:5000"
}

$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
$createUrl = "$ApiBaseUrl/api/users/create-simple"

$body = @{
    userName   = "tungchinhus"
    fullName   = "User Test Email"
    email      = "tungchinhus@gmail.com"
    password   = $Password
    roles      = @("User")
} | ConvertTo-Json

Write-Host "=== Tạo user test cho chức năng email ===" -ForegroundColor Green
Write-Host "  Email: tungchinhus@gmail.com" -ForegroundColor Cyan
Write-Host "  API:   $createUrl" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $createUrl -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "OK. User test da duoc tao." -ForegroundColor Green
    Write-Host "  UserId: $($response.userId)" -ForegroundColor White
    Write-Host "  Email:  $($response.email)" -ForegroundColor White
    Write-Host "  Roles:  $($response.roles -join ', ')" -ForegroundColor White
    Write-Host ""
    Write-Host "Dang nhap bang email: tungchinhus@gmail.com va mat khau da set (mac dinh: TestEmail123!)" -ForegroundColor Yellow
    exit 0
}
catch {
    $statusCode = $_.Exception.Response?.StatusCode?.value__
    $message = $_.ErrorDetails?.Message
    if ($message) {
        try {
            $err = $message | ConvertFrom-Json
            if ($err.message) { $message = $err.message }
        } catch { }
    }
    if ($statusCode -eq 400 -and ($message -match "already exists|Email already exists|Username already exists")) {
        Write-Host "User test da ton tai (email hoac username). Bo qua." -ForegroundColor Yellow
        Write-Host "  Email: tungchinhus@gmail.com" -ForegroundColor White
        exit 0
    }
    Write-Host "Loi: $message" -ForegroundColor Red
    Write-Host "Kiem tra backend dang chay tai $ApiBaseUrl" -ForegroundColor Yellow
    exit 1
}
