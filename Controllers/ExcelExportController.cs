using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.IO;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Helpers;

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

                // Helper function để extract mã TBKT base (bỏ suffix chữ cái)
                string ExtractTbktBaseCode(string? tbkt)
                {
                    if (string.IsNullOrWhiteSpace(tbkt))
                        return "";
                    
                    // Lấy phần số từ đầu, bỏ phần chữ cái ở cuối
                    // Ví dụ: "19134A" -> "19134", "19134D" -> "19134"
                    var match = System.Text.RegularExpressions.Regex.Match(tbkt, @"^(\d+)");
                    return match.Success ? match.Groups[1].Value : tbkt;
                }

                // Helper function để ghi một dòng dữ liệu vào Excel
                void WriteDataRow(ExcelWorksheet ws, int rowNum, StatisticsDataDto stats)
                {
                    ws.Cells[rowNum, 1].Value = stats.CongSuat ?? ""; // Công suất
                    ws.Cells[rowNum, 2].Value = stats.Tbkt ?? ""; // TBKT
                    ws.Cells[rowNum, 3].Value = FormatNumber(stats.SoMau) ?? 0; // Số mẫu

                    // Pk H1
                    ws.Cells[rowNum, 4].Value = FormatNumber(stats.PkH1Max);
                    ws.Cells[rowNum, 5].Value = FormatNumber(stats.PkH1TB);
                    ws.Cells[rowNum, 6].Value = FormatNumber(stats.PkH1Min);
                    ws.Cells[rowNum, 7].Value = FormatNumber(stats.PkH1Delta);

                    // Pk H2
                    ws.Cells[rowNum, 8].Value = FormatNumber(stats.PkH2Max);
                    ws.Cells[rowNum, 9].Value = FormatNumber(stats.PkH2TB);
                    ws.Cells[rowNum, 10].Value = FormatNumber(stats.PkH2Min);
                    ws.Cells[rowNum, 11].Value = FormatNumber(stats.PkH2Delta);

                    // Uk H1
                    ws.Cells[rowNum, 12].Value = FormatNumber(stats.UkH1Max);
                    ws.Cells[rowNum, 13].Value = FormatNumber(stats.UkH1TB);
                    ws.Cells[rowNum, 14].Value = FormatNumber(stats.UkH1Min);
                    ws.Cells[rowNum, 15].Value = FormatNumber(stats.UkH1Delta);

                    // Uk H2
                    ws.Cells[rowNum, 16].Value = FormatNumber(stats.UkH2Max);
                    ws.Cells[rowNum, 17].Value = FormatNumber(stats.UkH2TB);
                    ws.Cells[rowNum, 18].Value = FormatNumber(stats.UkH2Min);
                    ws.Cells[rowNum, 19].Value = FormatNumber(stats.UkH2Delta);
                }

                // Helper function để tạo summary row từ một nhóm dữ liệu
                StatisticsDataDto CreateSummaryRow(List<StatisticsDataDto> group)
                {
                    var summary = new StatisticsDataDto
                    {
                        CongSuat = group.FirstOrDefault()?.CongSuat ?? "",
                        Tbkt = ExtractTbktBaseCode(group.FirstOrDefault()?.Tbkt),
                        SoMau = group.Sum(s => s.SoMau ?? 0),
                        PkH1Max = group.Where(s => s.PkH1Max.HasValue).Sum(s => s.PkH1Max!.Value),
                        PkH1TB = group.Where(s => s.PkH1TB.HasValue).Sum(s => s.PkH1TB!.Value),
                        PkH1Min = group.Where(s => s.PkH1Min.HasValue).Sum(s => s.PkH1Min!.Value),
                        PkH1Delta = group.Where(s => s.PkH1Delta.HasValue).Sum(s => s.PkH1Delta!.Value),
                        PkH2Max = group.Where(s => s.PkH2Max.HasValue).Sum(s => s.PkH2Max!.Value),
                        PkH2TB = group.Where(s => s.PkH2TB.HasValue).Sum(s => s.PkH2TB!.Value),
                        PkH2Min = group.Where(s => s.PkH2Min.HasValue).Sum(s => s.PkH2Min!.Value),
                        PkH2Delta = group.Where(s => s.PkH2Delta.HasValue).Sum(s => s.PkH2Delta!.Value),
                        UkH1Max = group.Where(s => s.UkH1Max.HasValue).Sum(s => s.UkH1Max!.Value),
                        UkH1TB = group.Where(s => s.UkH1TB.HasValue).Sum(s => s.UkH1TB!.Value),
                        UkH1Min = group.Where(s => s.UkH1Min.HasValue).Sum(s => s.UkH1Min!.Value),
                        UkH1Delta = group.Where(s => s.UkH1Delta.HasValue).Sum(s => s.UkH1Delta!.Value),
                        UkH2Max = group.Where(s => s.UkH2Max.HasValue).Sum(s => s.UkH2Max!.Value),
                        UkH2TB = group.Where(s => s.UkH2TB.HasValue).Sum(s => s.UkH2TB!.Value),
                        UkH2Min = group.Where(s => s.UkH2Min.HasValue).Sum(s => s.UkH2Min!.Value),
                        UkH2Delta = group.Where(s => s.UkH2Delta.HasValue).Sum(s => s.UkH2Delta!.Value)
                    };

                    // Set null nếu không có giá trị nào
                    if (!group.Any(s => s.PkH1Max.HasValue)) summary.PkH1Max = null;
                    if (!group.Any(s => s.PkH1TB.HasValue)) summary.PkH1TB = null;
                    if (!group.Any(s => s.PkH1Min.HasValue)) summary.PkH1Min = null;
                    if (!group.Any(s => s.PkH1Delta.HasValue)) summary.PkH1Delta = null;
                    if (!group.Any(s => s.PkH2Max.HasValue)) summary.PkH2Max = null;
                    if (!group.Any(s => s.PkH2TB.HasValue)) summary.PkH2TB = null;
                    if (!group.Any(s => s.PkH2Min.HasValue)) summary.PkH2Min = null;
                    if (!group.Any(s => s.PkH2Delta.HasValue)) summary.PkH2Delta = null;
                    if (!group.Any(s => s.UkH1Max.HasValue)) summary.UkH1Max = null;
                    if (!group.Any(s => s.UkH1TB.HasValue)) summary.UkH1TB = null;
                    if (!group.Any(s => s.UkH1Min.HasValue)) summary.UkH1Min = null;
                    if (!group.Any(s => s.UkH1Delta.HasValue)) summary.UkH1Delta = null;
                    if (!group.Any(s => s.UkH2Max.HasValue)) summary.UkH2Max = null;
                    if (!group.Any(s => s.UkH2TB.HasValue)) summary.UkH2TB = null;
                    if (!group.Any(s => s.UkH2Min.HasValue)) summary.UkH2Min = null;
                    if (!group.Any(s => s.UkH2Delta.HasValue)) summary.UkH2Delta = null;

                    return summary;
                }

                // Nhóm dữ liệu theo mã TBKT base và ghi vào Excel với summary rows
                var groupedData = request.StatisticsData
                    .GroupBy(s => ExtractTbktBaseCode(s.Tbkt))
                    .ToList();

                int currentRow = dataStartRow;
                var allWrittenRows = new List<int>(); // Lưu các dòng đã ghi (không bao gồm summary rows)
                var summaryRowIndices = new List<int>(); // Lưu các dòng summary

                foreach (var group in groupedData)
                {
                    var groupList = group.ToList();
                    int groupStartRow = currentRow;

                    // Ghi các dòng dữ liệu trong nhóm
                    foreach (var stats in groupList)
                    {
                        WriteDataRow(worksheet, currentRow, stats);
                        allWrittenRows.Add(currentRow);
                        currentRow++;
                    }

                    // Nếu nhóm có nhiều hơn 2 dòng, thêm summary row
                    if (groupList.Count > 2)
                    {
                        var summaryRow = CreateSummaryRow(groupList);
                        WriteDataRow(worksheet, currentRow, summaryRow);
                        
                        // Format summary row (có thể thêm style khác biệt nếu cần)
                        var summaryRange = worksheet.Cells[currentRow, 1, currentRow, 19];
                        summaryRange.Style.Font.Bold = true;
                        summaryRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        summaryRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        
                        summaryRowIndices.Add(currentRow);
                        _logger?.LogDebug("Added summary row at {Row} for TBKT group: {TbktBase}", currentRow, group.Key);
                        currentRow++;
                    }
                }

                int totalDataRows = allWrittenRows.Count;

                // Merge cells cho Công suất theo nhóm khi có nhiều hàng cùng công suất
                // Chỉ merge các dòng dữ liệu (không bao gồm summary rows)
                // allWrittenRows chỉ chứa các dòng dữ liệu, không có summary rows
                if (allWrittenRows.Count > 0)
                {
                    int groupStartRow = allWrittenRows[0];
                    string? currentCongSuat = worksheet.Cells[groupStartRow, 1].Value?.ToString();

                    for (int i = 1; i < allWrittenRows.Count; i++)
                    {
                        int currentDataRow = allWrittenRows[i];
                        string? nextCongSuat = worksheet.Cells[currentDataRow, 1].Value?.ToString();

                        // So sánh công suất (case-insensitive, null-safe)
                        bool isSameCongSuat = string.Equals(
                            currentCongSuat ?? "",
                            nextCongSuat ?? "",
                            StringComparison.OrdinalIgnoreCase);

                        // Kiểm tra xem có summary row giữa groupStartRow và currentDataRow không
                        bool hasSummaryRowBetween = summaryRowIndices.Any(sr => sr > groupStartRow && sr < currentDataRow);

                        if (!isSameCongSuat || hasSummaryRowBetween)
                        {
                            // Kết thúc nhóm hiện tại
                            int groupEndRow = allWrittenRows[i - 1];
                            
                            if (groupEndRow > groupStartRow)
                            {
                                var mergedRange = worksheet.Cells[groupStartRow, 1, groupEndRow, 1];
                                mergedRange.Merge = true;
                                mergedRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                _logger?.LogDebug("Merged cells A{StartRow}:A{EndRow} for CongSuat: {CongSuat}",
                                    groupStartRow, groupEndRow, currentCongSuat);
                            }

                            // Bắt đầu nhóm mới
                            groupStartRow = currentDataRow;
                            currentCongSuat = nextCongSuat;
                        }
                    }

                    // Merge nhóm cuối cùng
                    int lastDataRow = allWrittenRows.Last();
                    if (lastDataRow > groupStartRow)
                    {
                        var mergedRange = worksheet.Cells[groupStartRow, 1, lastDataRow, 1];
                        mergedRange.Merge = true;
                        mergedRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        _logger?.LogDebug("Merged cells A{StartRow}:A{EndRow} for CongSuat: {CongSuat}",
                            groupStartRow, lastDataRow, currentCongSuat);
                    }
                }

                // Xử lý chart nếu có chartConfig
                if (request.ChartConfig != null && request.ChartConfig.ShowChart)
                {
                    var chartConfig = request.ChartConfig;
                    var xAxisColumn = chartConfig.XAxisColumn;
                    var yAxisColumn = chartConfig.YAxisColumn;

                    // Validate chart config
                    if (string.IsNullOrWhiteSpace(xAxisColumn) || string.IsNullOrWhiteSpace(yAxisColumn))
                    {
                        _logger?.LogWarning("Chart config thiếu xAxisColumn hoặc yAxisColumn");
                    }
                    else
                    {
                        // Lấy vị trí cột trong Excel
                        int? xColIndex = GetColumnIndex(xAxisColumn);
                        int? yColIndex = GetColumnIndex(yAxisColumn);

                        if (!xColIndex.HasValue || !yColIndex.HasValue)
                        {
                            _logger?.LogWarning("Không tìm thấy cột: {XAxisColumn} hoặc {YAxisColumn}", 
                                xAxisColumn, yAxisColumn);
                        }
                        else
                        {
                            // Tính toán dataEndRow dựa trên dòng cuối cùng (bao gồm summary rows nếu có)
                            int dataEndRow = currentRow - 1;

                            // Xóa chart cũ nếu có
                            worksheet.Drawings.Clear();

                            // Tạo chart mới - chỉ sử dụng dữ liệu từ các dòng không phải summary
                            // allWrittenRows đã không chứa summary rows
                            if (allWrittenRows.Count > 0)
                            {
                                int chartStartRow = allWrittenRows.Min();
                                int chartEndRow = allWrittenRows.Max();

                                var chart = worksheet.Drawings.AddChart("Chart1", eChartType.Line);
                                chart.Title.Text = "Biểu đồ thống kê";
                                chart.SetPosition(dataEndRow + 2, 0, 0, 0);
                                chart.SetSize(600, 400);

                                // Cấu hình trục
                                chart.YAxis.Title.Text = yAxisColumn;
                                chart.XAxis.Title.Text = xAxisColumn;

                                // Tạo range cho X-axis và Y-axis (chỉ các dòng dữ liệu)
                                var xRange = worksheet.Cells[chartStartRow, xColIndex.Value, chartEndRow, xColIndex.Value];
                                var yRange = worksheet.Cells[chartStartRow, yColIndex.Value, chartEndRow, yColIndex.Value];

                                // Thêm series vào chart
                                // EPPlus: Add(yValues, xValues) - Y values trước, X values (categories) sau
                                var series = chart.Series.Add(yRange, xRange);
                                // Đặt tên series
                                if (!string.IsNullOrWhiteSpace(chartConfig.YAxisOriginalColumn))
                                {
                                    series.Header = chartConfig.YAxisOriginalColumn;
                                }
                                else
                                {
                                    series.Header = yAxisColumn;
                                }

                                _logger?.LogInformation("Chart created successfully with X-axis: {XAxisColumn} (col {XCol}), Y-axis: {YAxisColumn} (col {YCol}), Rows: {StartRow}-{EndRow}",
                                    xAxisColumn, xColIndex.Value, yAxisColumn, yColIndex.Value, chartStartRow, chartEndRow);
                            }
                            else
                            {
                                _logger?.LogWarning("No data rows available for chart (all rows are summary rows)");
                            }
                        }
                    }
                }

                // Generate file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                // Tạo tên file với timestamp
                var timestamp = DateTimeHelper.NowVietnam().ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"Thong_ke_so_sanh_thong_so_{timestamp}.xlsx";

                _logger?.LogInformation("Excel file generated successfully: {FileName}, Data Rows: {DataRowCount}, Summary Rows: {SummaryRowCount}, Total Rows: {TotalRowCount}", 
                    fileName, totalDataRows, summaryRowIndices.Count, currentRow - dataStartRow);

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

    /// <summary>
    /// Map tên cột statistics data sang Excel column index (1-based)
    /// </summary>
    private static int? GetColumnIndex(string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return null;

        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "congSuat", 1 },   // A
            { "tbkt", 2 },       // B
            { "soMau", 3 },      // C
            { "pkH1Max", 4 },    // D
            { "pkH1TB", 5 },     // E
            { "pkH1Min", 6 },    // F
            { "pkH1Delta", 7 },  // G
            { "pkH2Max", 8 },    // H
            { "pkH2TB", 9 },     // I
            { "pkH2Min", 10 },   // J
            { "pkH2Delta", 11 }, // K
            { "ukH1Max", 12 },   // L
            { "ukH1TB", 13 },    // M
            { "ukH1Min", 14 },   // N
            { "ukH1Delta", 15 }, // O
            { "ukH2Max", 16 },   // P
            { "ukH2TB", 17 },    // Q
            { "ukH2Min", 18 },   // R
            { "ukH2Delta", 19 }  // S
        };

        return columnMap.TryGetValue(columnName, out int index) ? index : null;
    }
}

