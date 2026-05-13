using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Helpers;
using quanlyfilesBE.Models;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BaoCaoTuanAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BaoCaoTuanExcelExportService _excel;
    private readonly ILogger<BaoCaoTuanAdminController>? _logger;

    public BaoCaoTuanAdminController(
        ApplicationDbContext context,
        BaoCaoTuanExcelExportService excel,
        ILogger<BaoCaoTuanAdminController>? logger = null)
    {
        _context = context;
        _excel = excel;
        _logger = logger;
    }

    private bool IsAdminOrManager() => RoleHelper.IsAdministratorOrManager(User);

    public class BaoCaoTuanAdminListItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string NguoiLap { get; set; } = string.Empty;
        public string TuanBaoCao { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        /// <summary>COALESCE(UpdatedAt, CreatedAt) — dùng lọc tháng và sort.</summary>
        public DateTime CapNhat { get; set; }
    }

    private static string UserDisplayName(User u)
    {
        if (!string.IsNullOrWhiteSpace(u.FullName))
            return u.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(u.Email))
            return u.Email.Trim();
        if (!string.IsNullOrWhiteSpace(u.UserName))
            return u.UserName.Trim();
        return $"User #{u.UserId}";
    }

    private async Task<List<BaoCaoTuanMonthlyUser>> GetMonthlyMatrixUsersAsync(List<int> reportUserIds)
    {
        // Lấy cả user có báo cáo + toàn bộ designer active để không mất cột trong mẫu (vd: Châu chưa có dữ liệu tháng).
        return await _context.Users.AsNoTracking()
            .Where(u => reportUserIds.Contains(u.UserId) || (u.IsActive && u.IsDesigner))
            .Select(u => new BaoCaoTuanMonthlyUser
            {
                UserId = u.UserId,
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email
            })
            .ToListAsync();
    }

    /// <summary>Tất cả báo cáo tuần có ngày cập nhật trong tháng (calendar), mới nhất trước.</summary>
    [HttpGet("for-month")]
    public async Task<ActionResult<IEnumerable<BaoCaoTuanAdminListItemDto>>> GetForMonth(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (!IsAdminOrManager())
            return StatusCode(403, new { message = "Chỉ Administrator hoặc Manager mới xem được." });

        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            return BadRequest(new { message = "Tháng/năm không hợp lệ." });

        try
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endExclusive = start.AddMonths(1);

            var list = await _context.BaoCaoTuan.AsNoTracking()
                .Where(x => (x.UpdatedAt ?? x.CreatedAt) >= start && (x.UpdatedAt ?? x.CreatedAt) < endExclusive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Select(x => new BaoCaoTuanAdminListItemDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    NguoiLap = x.NguoiLap ?? "",
                    TuanBaoCao = x.TuanBaoCao,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    CapNhat = x.UpdatedAt ?? x.CreatedAt
                })
                .ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetForMonth BaoCaoTuan admin");
            return StatusCode(500, new { error = "Lỗi tải danh sách", message = ex.Message });
        }
    }

    [HttpGet("matrix-for-month")]
    public async Task<ActionResult<BaoCaoTuanMonthlyMatrix>> GetMatrixForMonth(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (!IsAdminOrManager())
            return StatusCode(403, new { message = "Chỉ Administrator hoặc Manager mới xem được." });
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            return BadRequest(new { message = "Tháng/năm không hợp lệ." });

        try
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endExclusive = start.AddMonths(1);

            var reports = await _context.BaoCaoTuan.AsNoTracking()
                .Where(x => (x.UpdatedAt ?? x.CreatedAt) >= start && (x.UpdatedAt ?? x.CreatedAt) < endExclusive)
                .ToListAsync();

            var userIds = reports.Select(r => r.UserId).Distinct().ToList();
            var users = await GetMonthlyMatrixUsersAsync(userIds);

            var matrix = _excel.BuildMonthlyMatrix(reports, users);
            return Ok(matrix);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMatrixForMonth");
            return StatusCode(500, new { error = "Lỗi tải ma trận báo cáo tháng", message = ex.Message });
        }
    }

    /// <summary>ZIP: mỗi báo cáo một file .xlsx theo mẫu BAO CAO TUAN (Thao).</summary>
    [HttpGet("export-weekly-zip")]
    public async Task<IActionResult> ExportWeeklyZip([FromQuery] int year, [FromQuery] int month)
    {
        if (!IsAdminOrManager())
            return StatusCode(403, new { message = "Chỉ Administrator hoặc Manager mới xuất được." });

        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            return BadRequest(new { message = "Tháng/năm không hợp lệ." });

        if (!_excel.WeeklyTemplateExists)
            return NotFound(new { message = "Chưa cấu hình file mẫu BAO CAO TUAN (Thao).xlsx trong assets/BaoCaoExcel." });

        try
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endExclusive = start.AddMonths(1);

            var reports = await _context.BaoCaoTuan.AsNoTracking()
                .Where(x => (x.UpdatedAt ?? x.CreatedAt) >= start && (x.UpdatedAt ?? x.CreatedAt) < endExclusive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToListAsync();

            if (reports.Count == 0)
                return BadRequest(new { message = "Không có báo cáo nào trong tháng để xuất." });

            var userIds = reports.Select(r => r.UserId).Distinct().ToList();
            var users = await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => UserDisplayName(u));

            string Name(BaoCaoTuan r)
            {
                if (!string.IsNullOrWhiteSpace(r.NguoiLap)) return r.NguoiLap.Trim();
                return users.TryGetValue(r.UserId, out var n) ? n : $"User{r.UserId}";
            }

            var zipBytes = _excel.BuildWeeklyZipArchive(reports, Name);
            var fileName = $"BaoCaoTuan_Thang{month}_{year}.zip";
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportWeeklyZip");
            return StatusCode(500, new { error = "Lỗi xuất ZIP", message = ex.Message });
        }
    }

    /// <summary>Một file .xlsx: toàn bộ dòng chi tiết các báo cáo có ngày cập nhật trong tuần lịch hiện tại (Thứ Hai–Chủ Nhật, giờ VN).</summary>
    [HttpGet("export-weekly-excel-current-week")]
    public async Task<IActionResult> ExportWeeklyExcelCurrentWeek()
    {
        if (!IsAdminOrManager())
            return StatusCode(403, new { message = "Chỉ Administrator hoặc Manager mới xuất được." });

        try
        {
            var todayVn = DateTimeHelper.NowVietnam().Date;
            var (weekStart, weekEndExclusive) = BaoCaoTuanExcelExportService.GetVietnamCalendarWeekRange(todayVn);
            var weekStartUnspec = DateTime.SpecifyKind(weekStart, DateTimeKind.Unspecified);
            var weekEndExclusiveUnspec = DateTime.SpecifyKind(weekEndExclusive, DateTimeKind.Unspecified);

            var reports = await _context.BaoCaoTuan.AsNoTracking()
                .Where(x => (x.UpdatedAt ?? x.CreatedAt) >= weekStartUnspec
                    && (x.UpdatedAt ?? x.CreatedAt) < weekEndExclusiveUnspec)
                .OrderBy(x => x.NguoiLap ?? "").ThenBy(x => x.TuanBaoCao)
                .ToListAsync();

            if (reports.Count == 0)
                return BadRequest(new { message = "Không có báo cáo nào trong tuần hiện tại để xuất." });

            var userIds = reports.Select(r => r.UserId).Distinct().ToList();
            var users = await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => UserDisplayName(u));

            string Name(BaoCaoTuan r)
            {
                if (!string.IsNullOrWhiteSpace(r.NguoiLap)) return r.NguoiLap.Trim();
                return users.TryGetValue(r.UserId, out var n) ? n : $"User{r.UserId}";
            }

            var sunday = weekStart.AddDays(6);
            var title =
                $"Báo cáo tuần (cập nhật trong khoảng {weekStart:dd/MM/yyyy} – {sunday:dd/MM/yyyy}, giờ VN)";
            var bytes = _excel.BuildWeeklyReportsFlatWorkbook(reports, Name, title);
            var fileName = $"BaoCaoTuan_Tuan_{weekStart:ddMMyyyy}_{sunday:ddMMyyyy}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportWeeklyExcelCurrentWeek");
            return StatusCode(500, new { error = "Lỗi xuất Excel", message = ex.Message });
        }
    }

    /// <summary>Một file .xlsx mẫu BAO CAO TONG HOP (Chinh) + sheet chi tiết toàn bộ dòng trong tháng.</summary>
    [HttpGet("export-monthly-workbook")]
    public async Task<IActionResult> ExportMonthlyWorkbook([FromQuery] int year, [FromQuery] int month)
    {
        if (!IsAdminOrManager())
            return StatusCode(403, new { message = "Chỉ Administrator hoặc Manager mới xuất được." });

        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            return BadRequest(new { message = "Tháng/năm không hợp lệ." });

        if (!_excel.MonthlyTemplateExists)
            return NotFound(new { message = "Chưa cấu hình file mẫu BAO CAO TONG HOP (Chinh).xlsx trong assets/BaoCaoExcel." });

        try
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endExclusive = start.AddMonths(1);

            var reports = await _context.BaoCaoTuan.AsNoTracking()
                .Where(x => (x.UpdatedAt ?? x.CreatedAt) >= start && (x.UpdatedAt ?? x.CreatedAt) < endExclusive)
                .OrderBy(x => x.NguoiLap ?? "").ThenBy(x => x.TuanBaoCao)
                .ToListAsync();

            if (reports.Count == 0)
                return BadRequest(new { message = "Không có báo cáo nào trong tháng để xuất." });

            var userIds = reports.Select(r => r.UserId).Distinct().ToList();
            var users = await GetMonthlyMatrixUsersAsync(userIds);

            var bytes = _excel.BuildMonthlyWorkbook(reports, users, year, month);
            var fileName = $"BaoCaoTongHop_Thang{month}_{year}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportMonthlyWorkbook");
            return StatusCode(500, new { error = "Lỗi xuất Excel", message = ex.Message });
        }
    }
}
