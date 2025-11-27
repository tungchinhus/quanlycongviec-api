# Script để chạy backend và hiển thị logs rõ ràng
Write-Host "========================================" -ForegroundColor Green
Write-Host "  BACKEND API - XEM LOGS CONSOLE" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Logs sẽ hiển thị ở đây khi có request." -ForegroundColor Yellow
Write-Host "Nhấn Ctrl+C để dừng backend." -ForegroundColor Yellow
Write-Host ""
Write-Host "Các logs bạn sẽ thấy:" -ForegroundColor Cyan
Write-Host "  - [Routing Debug] - Request đã đến routing" -ForegroundColor White
Write-Host "  - UpdateWorkItem called - Request đã đến controller" -ForegroundColor White
Write-Host "  - Total work items - Số lượng work items trong DB" -ForegroundColor White
Write-Host "  - All work item IDs - Danh sách tất cả IDs" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Set environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Chạy backend
dotnet run --launch-profile http


