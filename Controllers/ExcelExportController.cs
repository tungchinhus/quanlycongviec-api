using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using System.IO;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExcelExportController : ControllerBase
{
    private readonly ILogger<ExcelExportController>? _logger;
    private readonly IWebHostEnvironment _environment;

    public ExcelExportController(
        ILogger<ExcelExportController>? logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        
        // Set EPPlus license context (required for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Export Excel file với chart từ template
    /// </summary>
    /// <param name="request">Dữ liệu thống kê cần export</param>
    /// <returns>File Excel với chart</returns>
    [HttpPost("export-excel-with-chart")]
    public IActionResult ExportExcelWithChart([FromBody] ExcelExportRequest request)
    {
        try
        {
            if (request == null || request.StatisticsData == null || request.StatisticsData.Count == 0)
            {
                return BadRequest(new { error = "Dữ liệu thống kê không được để trống" });
            }

            // Đường dẫn đến template
            var templatePath = Path.Combine(_environment.ContentRootPath, "assets", "thongke_template.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                _logger?.LogError("Template file not found: {TemplatePath}", templatePath);
                return NotFound(new { error = "Không tìm thấy file template", path = templatePath });
            }

            // EPPlus giữ lại chart tốt
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                var worksheet = package.Workbook.Worksheets["Thống kê"];
                if (worksheet == null)
                {
                    // Nếu không tìm thấy worksheet "Thống kê", lấy worksheet đầu tiên
                    worksheet = package.Workbook.Worksheets[0];
                    _logger?.LogWarning("Worksheet 'Thống kê' not found, using first worksheet: {WorksheetName}", worksheet.Name);
                }

                int dataStartRow = 4; // Dòng bắt đầu ghi dữ liệu (1-based index)
                int lastRow = worksheet.Dimension?.End.Row ?? dataStartRow;

                // Clear dữ liệu cũ từ dòng 4 trở đi
                if (lastRow >= dataStartRow)
                {
                    worksheet.Cells[dataStartRow, 1, lastRow, 19].Clear();
                    _logger?.LogInformation("Cleared old data from row {StartRow} to {EndRow}", dataStartRow, lastRow);
                }

                // Helper function để format number
                double? FormatNumber(double? val)
                {
                    if (val == null) return null;
                    var rounded = Math.Round(val.Value * 100) / 100;
                    return rounded % 1 == 0 ? Math.Round(rounded) : rounded;
                }

                // Ghi dữ liệu mới
                for (int index = 0; index < request.StatisticsData.Count; index++)
                {
                    var stats = request.StatisticsData[index];
                    int rowNum = dataStartRow + index;

                    // Ghi dữ liệu vào các cột
                    worksheet.Cells[rowNum, 1].Value = stats.CongSuat ?? ""; // Công suất
                    worksheet.Cells[rowNum, 2].Value = stats.Tbkt ?? ""; // TBKT
                    worksheet.Cells[rowNum, 3].Value = FormatNumber(stats.SoMau) ?? 0; // Số mẫu

                    // Pk H1
                    worksheet.Cells[rowNum, 4].Value = FormatNumber(stats.PkH1Max);
                    worksheet.Cells[rowNum, 5].Value = FormatNumber(stats.PkH1TB);
                    worksheet.Cells[rowNum, 6].Value = FormatNumber(stats.PkH1Min);
                    worksheet.Cells[rowNum, 7].Value = FormatNumber(stats.PkH1Delta);

                    // Pk H2
                    worksheet.Cells[rowNum, 8].Value = FormatNumber(stats.PkH2Max);
                    worksheet.Cells[rowNum, 9].Value = FormatNumber(stats.PkH2TB);
                    worksheet.Cells[rowNum, 10].Value = FormatNumber(stats.PkH2Min);
                    worksheet.Cells[rowNum, 11].Value = FormatNumber(stats.PkH2Delta);

                    // Uk H1
                    worksheet.Cells[rowNum, 12].Value = FormatNumber(stats.UkH1Max);
                    worksheet.Cells[rowNum, 13].Value = FormatNumber(stats.UkH1TB);
                    worksheet.Cells[rowNum, 14].Value = FormatNumber(stats.UkH1Min);
                    worksheet.Cells[rowNum, 15].Value = FormatNumber(stats.UkH1Delta);

                    // Uk H2
                    worksheet.Cells[rowNum, 16].Value = FormatNumber(stats.UkH2Max);
                    worksheet.Cells[rowNum, 17].Value = FormatNumber(stats.UkH2TB);
                    worksheet.Cells[rowNum, 18].Value = FormatNumber(stats.UkH2Min);
                    worksheet.Cells[rowNum, 19].Value = FormatNumber(stats.UkH2Delta);
                }

                // Merge cells cho Công suất theo nhóm khi có nhiều hàng cùng công suất
                if (request.StatisticsData.Count > 0)
                {
                    int groupStartRow = dataStartRow;
                    string? currentCongSuat = request.StatisticsData[0].CongSuat;

                    for (int index = 1; index < request.StatisticsData.Count; index++)
                    {
                        var stats = request.StatisticsData[index];
                        string? nextCongSuat = stats.CongSuat;

                        // So sánh công suất (case-insensitive, null-safe)
                        bool isSameCongSuat = string.Equals(
                            currentCongSuat ?? "",
                            nextCongSuat ?? "",
                            StringComparison.OrdinalIgnoreCase);

                        if (!isSameCongSuat)
                        {
                            // Kết thúc nhóm hiện tại, merge nếu có nhiều hơn 1 hàng
                            int groupEndRow = dataStartRow + index - 1;
                            if (groupEndRow > groupStartRow)
                            {
                                var mergedRange = worksheet.Cells[groupStartRow, 1, groupEndRow, 1];
                                mergedRange.Merge = true;
                                // Căn giữa theo chiều dọc cho cell đã merge
                                mergedRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                _logger?.LogDebug("Merged cells A{StartRow}:A{EndRow} for CongSuat: {CongSuat}",
                                    groupStartRow, groupEndRow, currentCongSuat);
                            }

                            // Bắt đầu nhóm mới
                            groupStartRow = dataStartRow + index;
                            currentCongSuat = nextCongSuat;
                        }
                    }

                    // Merge nhóm cuối cùng
                    int lastGroupEndRow = dataStartRow + request.StatisticsData.Count - 1;
                    if (lastGroupEndRow > groupStartRow)
                    {
                        var mergedRange = worksheet.Cells[groupStartRow, 1, lastGroupEndRow, 1];
                        mergedRange.Merge = true;
                        // Căn giữa theo chiều dọc cho cell đã merge
                        mergedRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        _logger?.LogDebug("Merged cells A{StartRow}:A{EndRow} for CongSuat: {CongSuat}",
                            groupStartRow, lastGroupEndRow, currentCongSuat);
                    }
                }

                // Generate file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                // Tạo tên file với timestamp
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"Thong_ke_so_sanh_thong_so_{timestamp}.xlsx";

                _logger?.LogInformation("Excel file generated successfully: {FileName}, Rows: {RowCount}", fileName, request.StatisticsData.Count);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exporting Excel: {Message}", ex.Message);
            return StatusCode(500, new { error = "Lỗi khi xuất file Excel", details = ex.Message });
        }
    }
}

