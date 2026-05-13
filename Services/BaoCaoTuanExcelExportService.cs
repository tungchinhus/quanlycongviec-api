using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using OfficeOpenXml;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Services;

/// <summary>Xuất Excel báo cáo tuần theo mẫu Thao/Chinh — điền vùng dữ liệu hàng 8–67, cột D–G (vướng mắc, đề xuất/ghi chú, kết quả, tổng SL).</summary>
public class BaoCaoTuanExcelExportService
{
    private static readonly string[] FixedStaffLabels =
    {
        "L.Khôi", "Châu", "Nghĩa", "Lân", "D.Thanh", "Thành", "Hiếu", "M.Thanh",
        "Việt", "Dũng", "Tuấn", "Tú", "Cường", "Hòa", "V.Khôi"
    };
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BaoCaoTuanExcelExportService>? _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BaoCaoTuanExcelExportService(IWebHostEnvironment env, ILogger<BaoCaoTuanExcelExportService>? logger = null)
    {
        _env = env;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private string WeeklyTemplatePath =>
        Path.Combine(_env.ContentRootPath, "assets", "BaoCaoExcel", "BAO CAO TUAN (Thao).xlsx");

    private string MonthlyTemplatePath
    {
        get
        {
            // Ưu tiên template "tempt" nếu có để đúng bố cục user đang dùng.
            var preferred = Path.Combine(_env.ContentRootPath, "assets", "BaoCaoExcel", "BAO CAO TONG HOP tempt.xlsx");
            if (File.Exists(preferred)) return preferred;
            return Path.Combine(_env.ContentRootPath, "assets", "BaoCaoExcel", "BAO CAO TONG HOP (Chinh).xlsx");
        }
    }

    public bool WeeklyTemplateExists => File.Exists(WeeklyTemplatePath);
    public bool MonthlyTemplateExists => File.Exists(MonthlyTemplatePath);

    public static string SanitizeFilePart(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "unknown";
        var invalid = Path.GetInvalidFileNameChars();
        var chars = s.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var t = new string(chars);
        return t.Length > 80 ? t[..80] : t;
    }

    private static List<BaoCaoTuanRowExport> ParseRows(string rowsJson)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<BaoCaoTuanRowExport>>(rowsJson, JsonOpts);
            return list ?? new List<BaoCaoTuanRowExport>();
        }
        catch
        {
            return new List<BaoCaoTuanRowExport>();
        }
    }

    /// <summary>Điền sheet đầu: tiêu đề A4 (vùng merge), D–G từ hàng 8.</summary>
    public void FillTemplateSheet(ExcelWorksheet ws, string titleRow4, IReadOnlyList<BaoCaoTuanRowExport> rows)
    {
        ws.Cells["A4"].Value = titleRow4;
        const int startRow = 8;
        const int maxRows = 60;
        var n = Math.Min(rows.Count, maxRows);
        for (var i = 0; i < n; i++)
        {
            var r = startRow + i;
            var row = rows[i];
            ws.Cells[r, 4].Value = row.VuongMac ?? "";
            ws.Cells[r, 5].Value = row.DeXuat ?? "";
            ws.Cells[r, 6].Value = row.KetQua ?? "";
            var ts = row.TongSo?.Trim();
            if (!string.IsNullOrEmpty(ts))
            {
                var digits = new string(ts.Where(char.IsDigit).ToArray());
                if (digits.Length > 0 && long.TryParse(digits, out var num))
                    ws.Cells[r, 7].Value = num;
                else
                    ws.Cells[r, 7].Value = ts;
            }
            else
                ws.Cells[r, 7].Value = null;
        }
    }

    public byte[] BuildWeeklyFilledWorkbook(BaoCaoTuan entity, string displayUserName)
    {
        if (!WeeklyTemplateExists)
            throw new FileNotFoundException("Không tìm thấy mẫu BAO CAO TUAN (Thao).xlsx", WeeklyTemplatePath);

        using var fs = new FileStream(WeeklyTemplatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        ms.Position = 0;

        using var package = new ExcelPackage(ms);
        var ws = package.Workbook.Worksheets[0];
        var title = $"{entity.TuanBaoCao} — {displayUserName}";
        FillTemplateSheet(ws, title, ParseRows(entity.RowsJson));
        return package.GetAsByteArray();
    }

    private static string NormalizeKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }

        var noAccent = sb.ToString()
            .Replace('đ', 'd')
            .Replace(" ", "")
            .Replace(".", "")
            .Replace("-", "")
            .Replace("_", "");
        return noAccent;
    }

    private static bool TryParseTongSo(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var digits = new string(raw.Trim().Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return false;
        if (!double.TryParse(digits, out value)) return false;
        return true;
    }

    private static IEnumerable<string> BuildUserMatchKeys(BaoCaoTuanMonthlyUser user)
    {
        var full = (user.FullName ?? "").Trim();
        if (!string.IsNullOrEmpty(full))
        {
            yield return NormalizeKey(full);

            var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0)
            {
                var last = parts[^1];
                yield return NormalizeKey(last);

                if (parts.Length >= 2)
                {
                    var firstInitial = parts[0][0].ToString();
                    yield return NormalizeKey($"{firstInitial}.{last}");
                    yield return NormalizeKey($"{firstInitial}{last}");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
            yield return NormalizeKey(user.UserName);
        if (!string.IsNullOrWhiteSpace(user.Email))
            yield return NormalizeKey(user.Email.Split('@')[0]);
    }

    private static Dictionary<int, int> ResolveUserToColumn(
        ExcelWorksheet ws,
        IReadOnlyList<BaoCaoTuanMonthlyUser> users)
    {
        // Mẫu tổng hợp: hàng tiêu đề user ở dòng 6, cột F..R.
        const int headerRow = 6;
        const int colStart = 6; // F
        const int colEnd = 20;  // T

        var columnByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = colStart; c <= colEnd; c++)
        {
            var text = ws.Cells[headerRow, c].Text?.Trim() ?? "";
            var key = NormalizeKey(text);
            if (string.IsNullOrEmpty(key)) continue;
            if (!columnByKey.ContainsKey(key))
                columnByKey[key] = c;
        }

        var map = new Dictionary<int, int>();
        foreach (var user in users)
        {
            var found = -1;
            foreach (var k in BuildUserMatchKeys(user))
            {
                if (columnByKey.TryGetValue(k, out var col))
                {
                    found = col;
                    break;
                }
            }

            if (found > 0)
                map[user.UserId] = found;
        }

        // Fallback: gán user chưa map vào các cột header trống (thường S/T trong mẫu tháng).
        var emptyCols = new List<int>();
        for (var c = colStart; c <= colEnd; c++)
        {
            var text = ws.Cells[headerRow, c].Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                emptyCols.Add(c);
        }

        IEnumerable<BaoCaoTuanMonthlyUser> OrderUnmapped(IEnumerable<BaoCaoTuanMonthlyUser> xs)
        {
            var list = xs.ToList();
            // Ưu tiên 2 user theo yêu cầu hiển thị.
            var lk = list.FirstOrDefault(u => NormalizeKey(u.FullName) == "nguyenlekhoi");
            if (lk != null) { yield return lk; list.Remove(lk); }
            var ch = list.FirstOrDefault(u => NormalizeKey(u.FullName) == "tranphuocngocchau");
            if (ch != null) { yield return ch; list.Remove(ch); }
            foreach (var u in list) yield return u;
        }

        var unmapped = users.Where(u => !map.ContainsKey(u.UserId)).ToList();
        var idx = 0;
        foreach (var u in OrderUnmapped(unmapped))
        {
            if (idx >= emptyCols.Count) break;
            map[u.UserId] = emptyCols[idx++];
        }

        return map;
    }

    private static string BuildStaffDisplayLabel(BaoCaoTuanMonthlyUser? user)
    {
        if (user == null) return string.Empty;
        var full = (user.FullName ?? "").Trim();
        var norm = NormalizeKey(full);
        if (norm == "systemadministrator") return string.Empty;
        if (norm == "nguyenlekhoi") return "L.Khôi";
        if (norm == "tranphuocngocchau") return "Châu";

        if (!string.IsNullOrEmpty(full))
        {
            var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                var firstInitial = parts[0][0].ToString().ToUpperInvariant();
                var last = parts[^1];
                return $"{firstInitial}.{last}";
            }
            return full;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName.Trim();
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email.Split('@')[0];
        return $"User{user.UserId}";
    }

    private static int StaffOrderRank(string normalizedLabel)
    {
        for (var i = 0; i < FixedStaffLabels.Length; i++)
            if (normalizedLabel == NormalizeKey(FixedStaffLabels[i])) return i;
        return int.MaxValue;
    }

    public byte[] BuildMonthlyWorkbook(
        IReadOnlyList<BaoCaoTuan> reportsInMonth,
        IReadOnlyList<BaoCaoTuanMonthlyUser> users,
        int year,
        int month)
    {
        if (!MonthlyTemplateExists)
            throw new FileNotFoundException("Không tìm thấy mẫu BAO CAO TONG HOP (Chinh).xlsx", MonthlyTemplatePath);

        using var fs = new FileStream(MonthlyTemplatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        ms.Position = 0;

        using var package = new ExcelPackage(ms);
        var ws0 = package.Workbook.Worksheets[0];
        ws0.Cells["A4"].Value = $"TỔNG HỢP BÁO CÁO THÁNG {month:00}/{year} — xuất từ hệ thống";

        // Vùng dữ liệu theo mẫu: hàng 8..67, cột F..R là cột nhân sự.
        const int dataStartRow = 8;
        const int maxRows = 60;
        const int staffColStart = 6; // F
        const int staffColEnd = 20;  // T
        ws0.Cells[dataStartRow, staffColStart, dataStartRow + maxRows - 1, staffColEnd].Clear();

        var userColumnMap = ResolveUserToColumn(ws0, users);
        foreach (var rep in reportsInMonth)
        {
            if (!userColumnMap.TryGetValue(rep.UserId, out var targetCol))
            {
                _logger?.LogWarning("Không map được user {UserId}/{NguoiLap} vào cột nhân viên trong mẫu tháng.", rep.UserId, rep.NguoiLap);
                continue;
            }

            var rows = ParseRows(rep.RowsJson);
            var n = Math.Min(rows.Count, maxRows);
            for (var i = 0; i < n; i++)
            {
                if (!TryParseTongSo(rows[i].TongSo, out var qty)) continue;
                var excelRow = dataStartRow + i;
                var current = ws0.Cells[excelRow, targetCol].Value;
                var baseVal = 0d;
                if (current != null && double.TryParse(current.ToString(), out var cur))
                    baseVal = cur;
                ws0.Cells[excelRow, targetCol].Value = baseVal + qty;
            }
        }

        const string detailName = "ChiTietHeThong";
        ExcelWorksheet? wsDetail = null;
        foreach (var w in package.Workbook.Worksheets)
        {
            if (w.Name == detailName)
            {
                wsDetail = w;
                break;
            }
        }

        wsDetail ??= package.Workbook.Worksheets.Add(detailName);
        var d = wsDetail;
        d.Cells[1, 1].Value = "Người lập";
        d.Cells[1, 2].Value = "Tuần báo cáo";
        d.Cells[1, 3].Value = "Ngày cập nhật";
        d.Cells[1, 4].Value = "STT";
        d.Cells[1, 5].Value = "Danh mục 1";
        d.Cells[1, 6].Value = "Danh mục 2";
        d.Cells[1, 7].Value = "Kết quả thực hiện";
        d.Cells[1, 8].Value = "Tổng SL";
        d.Cells[1, 9].Value = "Vướng mắc";
        d.Cells[1, 10].Value = "Đề xuất";

        var rr = 2;
        foreach (var rep in reportsInMonth.OrderBy(x => x.NguoiLap).ThenBy(x => x.TuanBaoCao))
        {
            var cap = rep.UpdatedAt ?? rep.CreatedAt;
            var display = string.IsNullOrWhiteSpace(rep.NguoiLap) ? $"User#{rep.UserId}" : rep.NguoiLap.Trim();
            foreach (var row in ParseRows(rep.RowsJson))
            {
                d.Cells[rr, 1].Value = display;
                d.Cells[rr, 2].Value = rep.TuanBaoCao;
                d.Cells[rr, 3].Value = cap;
                d.Cells[rr, 4].Value = row.Stt;
                d.Cells[rr, 5].Value = row.DanhMuc1 ?? "";
                d.Cells[rr, 6].Value = row.DanhMuc2 ?? "";
                d.Cells[rr, 7].Value = row.KetQua ?? "";
                d.Cells[rr, 8].Value = row.TongSo ?? "";
                d.Cells[rr, 9].Value = row.VuongMac ?? "";
                d.Cells[rr, 10].Value = row.DeXuat ?? "";
                rr++;
            }
        }

        d.Cells[1, 1, 1, 10].Style.Font.Bold = true;
        return package.GetAsByteArray();
    }

    public BaoCaoTuanMonthlyMatrix BuildMonthlyMatrix(
        IReadOnlyList<BaoCaoTuan> reportsInMonth,
        IReadOnlyList<BaoCaoTuanMonthlyUser> users)
    {
        if (!MonthlyTemplateExists)
            throw new FileNotFoundException("Không tìm thấy mẫu BAO CAO TONG HOP tháng", MonthlyTemplatePath);

        using var fs = new FileStream(MonthlyTemplatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var package = new ExcelPackage(fs);
        var ws = package.Workbook.Worksheets[0];

        const int dataStartRow = 8;
        const int maxRows = 60;
        const int staffColStart = 6; // F
        const int staffColEnd = 20;  // T

        var matrix = new BaoCaoTuanMonthlyMatrix();
        for (var c = staffColStart; c <= staffColEnd; c++)
        {
            matrix.StaffColumns.Add(new BaoCaoTuanMonthlyStaffColumn
            {
                ColumnIndex = c,
                Label = ws.Cells[6, c].Text?.Trim() ?? $"Col{c}"
            });
        }

        for (var i = 0; i < maxRows; i++)
        {
            var excelRow = dataStartRow + i;
            matrix.Rows.Add(new BaoCaoTuanMonthlyMatrixRow
            {
                RowIndex = i,
                Stt = ws.Cells[excelRow, 1].Text?.Trim() ?? "",
                DanhMuc1 = ws.Cells[excelRow, 2].Text?.Trim() ?? "",
                DanhMuc2 = ws.Cells[excelRow, 3].Text?.Trim() ?? ""
            });
        }

        var userColumnMap = ResolveUserToColumn(ws, users);
        var userById = users.ToDictionary(x => x.UserId, x => x);
        var staffByColumn = matrix.StaffColumns.ToDictionary(x => x.ColumnIndex, x => x);
        foreach (var kv in userColumnMap)
        {
            if (staffByColumn.TryGetValue(kv.Value, out var col))
            {
                col.UserId = kv.Key;
                if (string.IsNullOrWhiteSpace(col.Label) || col.Label.StartsWith("Col", StringComparison.OrdinalIgnoreCase))
                {
                    userById.TryGetValue(kv.Key, out var u);
                    col.Label = BuildStaffDisplayLabel(u);
                }
            }
        }

        // Cột hiển thị cố định đủ tên theo chuẩn, dù tháng đó có dữ liệu hay không.
        var byLabel = matrix.StaffColumns
            .GroupBy(c => NormalizeKey(c.Label))
            .ToDictionary(g => g.Key, g => g.First());

        var fixedCols = new List<BaoCaoTuanMonthlyStaffColumn>();
        var syntheticIndex = 1000;
        foreach (var label in FixedStaffLabels)
        {
            var key = NormalizeKey(label);
            if (byLabel.TryGetValue(key, out var existing))
            {
                existing.Label = label;
                fixedCols.Add(existing);
            }
            else
            {
                // Không có trong template/map tháng -> vẫn tạo cột rỗng để hiển thị.
                fixedCols.Add(new BaoCaoTuanMonthlyStaffColumn
                {
                    ColumnIndex = syntheticIndex++,
                    Label = label,
                    UserId = null
                });
            }
        }
        matrix.StaffColumns = fixedCols;

        foreach (var rep in reportsInMonth)
        {
            if (!userColumnMap.TryGetValue(rep.UserId, out var targetCol)) continue;
            var rows = ParseRows(rep.RowsJson);
            var n = Math.Min(rows.Count, maxRows);
            for (var i = 0; i < n; i++)
            {
                if (!TryParseTongSo(rows[i].TongSo, out var qty)) continue;
                if (!matrix.Rows[i].Values.TryGetValue(targetCol, out var cur)) cur = 0;
                matrix.Rows[i].Values[targetCol] = cur + qty;
            }
        }

        return matrix;
    }

    public byte[] BuildWeeklyZipArchive(IReadOnlyList<BaoCaoTuan> reports, Func<BaoCaoTuan, string> displayNameFor)
    {
        using var zipMs = new MemoryStream();
        using (var zip = new ZipArchive(zipMs, ZipArchiveMode.Create, true))
        {
            foreach (var rep in reports)
            {
                var name = SanitizeFilePart($"{displayNameFor(rep)}_{rep.TuanBaoCao}_{rep.Id}") + ".xlsx";
                var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Fastest);
                using var es = entry.Open();
                var bytes = BuildWeeklyFilledWorkbook(rep, displayNameFor(rep));
                es.Write(bytes, 0, bytes.Length);
            }
        }

        return zipMs.ToArray();
    }

    /// <summary>Một file .xlsx: tất cả dòng chi tiết của các báo cáo tuần trong khoảng thời gian (sheet «DuLieu»).</summary>
    public byte[] BuildWeeklyReportsFlatWorkbook(
        IReadOnlyList<BaoCaoTuan> reports,
        Func<BaoCaoTuan, string> displayNameFor,
        string sheetTitle)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("DuLieu");
        ws.Cells[1, 1].Value = sheetTitle;
        ws.Cells[1, 1, 1, 11].Merge = true;
        ws.Cells[1, 1].Style.Font.Bold = true;
        ws.Cells[1, 1].Style.Font.Size = 12;

        const int headerRow = 3;
        ws.Cells[headerRow, 1].Value = "Người lập";
        ws.Cells[headerRow, 2].Value = "UserId";
        ws.Cells[headerRow, 3].Value = "Tuần báo cáo";
        ws.Cells[headerRow, 4].Value = "Ngày cập nhật";
        ws.Cells[headerRow, 5].Value = "STT";
        ws.Cells[headerRow, 6].Value = "Danh mục 1";
        ws.Cells[headerRow, 7].Value = "Danh mục 2";
        ws.Cells[headerRow, 8].Value = "Kết quả thực hiện";
        ws.Cells[headerRow, 9].Value = "Tổng SL";
        ws.Cells[headerRow, 10].Value = "Vướng mắc";
        ws.Cells[headerRow, 11].Value = "Đề xuất";
        ws.Cells[headerRow, 1, headerRow, 11].Style.Font.Bold = true;

        var r = headerRow + 1;
        foreach (var rep in reports.OrderBy(x => displayNameFor(x)).ThenBy(x => x.TuanBaoCao))
        {
            var cap = rep.UpdatedAt ?? rep.CreatedAt;
            var name = displayNameFor(rep);
            foreach (var row in ParseRows(rep.RowsJson))
            {
                ws.Cells[r, 1].Value = name;
                ws.Cells[r, 2].Value = rep.UserId;
                ws.Cells[r, 3].Value = rep.TuanBaoCao;
                ws.Cells[r, 4].Value = cap;
                ws.Cells[r, 5].Value = row.Stt;
                ws.Cells[r, 6].Value = row.DanhMuc1 ?? "";
                ws.Cells[r, 7].Value = row.DanhMuc2 ?? "";
                ws.Cells[r, 8].Value = row.KetQua ?? "";
                ws.Cells[r, 9].Value = row.TongSo ?? "";
                ws.Cells[r, 10].Value = row.VuongMac ?? "";
                ws.Cells[r, 11].Value = row.DeXuat ?? "";
                r++;
            }
        }

        if (ws.Dimension != null)
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    /// <summary>Thứ Hai 00:00 đến trước thứ Hai tuần sau (theo ngày lịch, giờ VN).</summary>
    public static (DateTime startInclusive, DateTime endExclusive) GetVietnamCalendarWeekRange(DateTime vietnamNowDate)
    {
        var d = vietnamNowDate.Date;
        var offset = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = d.AddDays(-offset);
        return (monday, monday.AddDays(7));
    }
}

public class BaoCaoTuanMonthlyUser
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
}

public class BaoCaoTuanMonthlyMatrix
{
    public List<BaoCaoTuanMonthlyStaffColumn> StaffColumns { get; set; } = new();
    public List<BaoCaoTuanMonthlyMatrixRow> Rows { get; set; } = new();
}

public class BaoCaoTuanMonthlyStaffColumn
{
    public int ColumnIndex { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? UserId { get; set; }
}

public class BaoCaoTuanMonthlyMatrixRow
{
    public int RowIndex { get; set; }
    public string Stt { get; set; } = string.Empty;
    public string DanhMuc1 { get; set; } = string.Empty;
    public string DanhMuc2 { get; set; } = string.Empty;
    public Dictionary<int, double> Values { get; set; } = new();
}

public class BaoCaoTuanRowExport
{
    public int? Stt { get; set; }
    public string? DanhMuc1 { get; set; }
    public string? DanhMuc2 { get; set; }
    public string? KetQua { get; set; }
    public string? TongSo { get; set; }
    public string? VuongMac { get; set; }
    public string? DeXuat { get; set; }
}
