using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BaoCaoTuanController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BaoCaoTuanController>? _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public BaoCaoTuanController(ApplicationDbContext context, ILogger<BaoCaoTuanController>? logger = null)
    {
        _context = context;
        _logger = logger;
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

    private async Task<User?> GetCurrentUserAsync()
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim();
        var email = User.FindFirst(ClaimTypes.Email)?.Value?.Trim();
        if (!string.IsNullOrEmpty(uid))
        {
            var u = await _context.Users.FirstOrDefaultAsync(x => x.FirebaseUID == uid);
            if (u != null) return u;
        }
        if (!string.IsNullOrEmpty(email))
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        return null;
    }

    public class BaoCaoTuanListItemDto
    {
        public int Id { get; set; }
        public string TuanBaoCao { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BaoCaoTuanDetailDto : BaoCaoTuanListItemDto
    {
        public JsonElement Rows { get; set; }
    }

    public class BaoCaoTuanSaveDto
    {
        public string TuanBaoCao { get; set; } = string.Empty;
        public JsonElement Rows { get; set; }
    }

    /// <summary>Lọc theo ngày hiển thị cột «Cập nhật»: <c>COALESCE(UpdatedAt, CreatedAt)</c> (cả ngày, inclusive).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<BaoCaoTuanListItemDto>>> GetMy(
        [FromQuery] DateOnly? tuNgay = null,
        [FromQuery] DateOnly? denNgay = null)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Không xác định được người dùng trong hệ thống." });

            if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value > denNgay.Value)
                return BadRequest(new { message = "Từ ngày không được sau đến ngày." });

            var query = _context.BaoCaoTuan
                .AsNoTracking()
                .Where(x => x.UserId == user.UserId);

            if (tuNgay.HasValue)
            {
                var start = tuNgay.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => (x.UpdatedAt ?? x.CreatedAt) >= start);
            }

            if (denNgay.HasValue)
            {
                var endExclusive = denNgay.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => (x.UpdatedAt ?? x.CreatedAt) < endExclusive);
            }

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Select(x => new BaoCaoTuanListItemDto
                {
                    Id = x.Id,
                    TuanBaoCao = x.TuanBaoCao,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMy BaoCaoTuan");
            return StatusCode(500, new { error = "Lỗi tải danh sách báo cáo", message = ex.Message });
        }
    }

    [HttpGet("my/{id:int}")]
    public async Task<ActionResult<BaoCaoTuanDetailDto>> GetMyById(int id)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Không xác định được người dùng trong hệ thống." });

            var entity = await _context.BaoCaoTuan.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.UserId);
            if (entity == null)
                return NotFound(new { message = "Không tìm thấy báo cáo" });

            JsonElement rows;
            try
            {
                rows = JsonSerializer.Deserialize<JsonElement>(entity.RowsJson);
            }
            catch
            {
                rows = JsonSerializer.SerializeToElement(Array.Empty<object>(), JsonOpts);
            }

            return Ok(new BaoCaoTuanDetailDto
            {
                Id = entity.Id,
                TuanBaoCao = entity.TuanBaoCao,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Rows = rows
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMyById BaoCaoTuan {Id}", id);
            return StatusCode(500, new { error = "Lỗi tải báo cáo", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<BaoCaoTuanDetailDto>> Create([FromBody] BaoCaoTuanSaveDto dto)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Không xác định được người dùng trong hệ thống." });

            var tuan = (dto.TuanBaoCao ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(tuan))
                return BadRequest(new { message = "Vui lòng nhập tuần báo cáo." });

            var exists = await _context.BaoCaoTuan.AnyAsync(x => x.UserId == user.UserId && x.TuanBaoCao == tuan);
            if (exists)
                return Conflict(new { message = "Bạn đã có báo cáo cho tuần này. Chọn bản ghi để sửa hoặc đổi khoảng tuần." });

            var rowsJson = dto.Rows.ValueKind == JsonValueKind.Undefined || dto.Rows.ValueKind == JsonValueKind.Null
                ? "[]"
                : JsonSerializer.Serialize(dto.Rows, JsonOpts);

            var now = Helpers.DateTimeHelper.NowVietnam();
            var lapName = UserDisplayName(user);
            var entity = new BaoCaoTuan
            {
                UserId = user.UserId,
                TuanBaoCao = tuan,
                NgayLap = now.Date,
                NguoiLap = lapName,
                RowsJson = rowsJson,
                CreatedAt = now,
                UpdatedAt = now,
                CapNhatBoiUserId = user.UserId,
                NguoiCapNhat = lapName
            };

            _context.BaoCaoTuan.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyById), new { id = entity.Id }, new BaoCaoTuanDetailDto
            {
                Id = entity.Id,
                TuanBaoCao = entity.TuanBaoCao,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Rows = JsonSerializer.Deserialize<JsonElement>(entity.RowsJson)
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Create BaoCaoTuan");
            return StatusCode(500, new { error = "Lỗi lưu báo cáo", message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaoCaoTuanDetailDto>> Update(int id, [FromBody] BaoCaoTuanSaveDto dto)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Không xác định được người dùng trong hệ thống." });

            var entity = await _context.BaoCaoTuan.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.UserId);
            if (entity == null)
                return NotFound(new { message = "Không tìm thấy báo cáo" });

            var tuan = (dto.TuanBaoCao ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(tuan))
                return BadRequest(new { message = "Vui lòng nhập tuần báo cáo." });

            var duplicate = await _context.BaoCaoTuan.AnyAsync(x =>
                x.UserId == user.UserId && x.TuanBaoCao == tuan && x.Id != id);
            if (duplicate)
                return Conflict(new { message = "Đã có báo cáo khác trùng khoảng tuần này." });

            var now = Helpers.DateTimeHelper.NowVietnam();
            if (entity.NgayLap is null)
                entity.NgayLap = now.Date;
            if (string.IsNullOrWhiteSpace(entity.NguoiLap))
                entity.NguoiLap = UserDisplayName(user);

            entity.TuanBaoCao = tuan;
            entity.RowsJson = dto.Rows.ValueKind == JsonValueKind.Undefined || dto.Rows.ValueKind == JsonValueKind.Null
                ? "[]"
                : JsonSerializer.Serialize(dto.Rows, JsonOpts);
            entity.UpdatedAt = now;
            entity.CapNhatBoiUserId = user.UserId;
            entity.NguoiCapNhat = UserDisplayName(user);

            await _context.SaveChangesAsync();

            return Ok(new BaoCaoTuanDetailDto
            {
                Id = entity.Id,
                TuanBaoCao = entity.TuanBaoCao,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Rows = JsonSerializer.Deserialize<JsonElement>(entity.RowsJson)
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Update BaoCaoTuan {Id}", id);
            return StatusCode(500, new { error = "Lỗi cập nhật báo cáo", message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Không xác định được người dùng trong hệ thống." });

            var entity = await _context.BaoCaoTuan.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.UserId);
            if (entity == null)
                return NotFound(new { message = "Không tìm thấy báo cáo" });

            _context.BaoCaoTuan.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Delete BaoCaoTuan {Id}", id);
            return StatusCode(500, new { error = "Lỗi xóa báo cáo", message = ex.Message });
        }
    }
}
