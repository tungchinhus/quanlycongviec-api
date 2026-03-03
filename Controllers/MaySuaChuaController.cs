using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaySuaChuaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaySuaChuaController>? _logger;

    public MaySuaChuaController(
        ApplicationDbContext context,
        ILogger<MaySuaChuaController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaySuaChua>>> GetAll(
        [FromQuery] int nam,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.MaySuaChua
                .Where(x => x.Nam == nam)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.SoTNTT_DV_DH_PKD != null && x.SoTNTT_DV_DH_PKD.ToLower().Contains(term)) ||
                    (x.ThongTinKhachHang != null && x.ThongTinKhachHang.ToLower().Contains(term)) ||
                    (x.SkVA != null && x.SkVA.ToLower().Contains(term)) ||
                    (x.DienAp != null && x.DienAp.ToLower().Contains(term)) ||
                    (x.NguoiThucHien != null && x.NguoiThucHien.ToLower().Contains(term)) ||
                    (x.SoMay != null && x.SoMay.ToLower().Contains(term)) ||
                    (x.SoTBKTSua != null && x.SoTBKTSua.ToLower().Contains(term)) ||
                    (x.GiaoPKD != null && x.GiaoPKD.ToLower().Contains(term)) ||
                    (x.GhiChu != null && x.GhiChu.ToLower().Contains(term)));
            }
            var items = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting MaySuaChua list for year {Nam}", nam);
            return StatusCode(500, new { error = "Lỗi tải danh sách", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MaySuaChua>> GetById(int id)
    {
        try
        {
            var item = await _context.MaySuaChua.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting MaySuaChua by Id: {Id}", id);
            return StatusCode(500, new { error = "Lỗi tải bản ghi", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MaySuaChua>> Create([FromBody] MaySuaChua dto)
    {
        try
        {
            var entity = new MaySuaChua
            {
                Nam = dto.Nam,
                SoTNTT_DV_DH_PKD = dto.SoTNTT_DV_DH_PKD,
                ThongTinKhachHang = dto.ThongTinKhachHang,
                SkVA = dto.SkVA,
                DienAp = dto.DienAp,
                NgayNhan = dto.NgayNhan,
                NguoiThucHien = dto.NguoiThucHien,
                SoMay = dto.SoMay,
                SoTBKTSua = dto.SoTBKTSua,
                GiaoPKD = dto.GiaoPKD,
                GhiChu = dto.GhiChu
            };
            _context.MaySuaChua.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating MaySuaChua");
            return StatusCode(500, new { error = "Lỗi thêm mới", message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MaySuaChua>> Update(int id, [FromBody] MaySuaChua dto)
    {
        try
        {
            var existing = await _context.MaySuaChua.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });

            existing.Nam = dto.Nam;
            existing.SoTNTT_DV_DH_PKD = dto.SoTNTT_DV_DH_PKD ?? existing.SoTNTT_DV_DH_PKD;
            existing.ThongTinKhachHang = dto.ThongTinKhachHang ?? existing.ThongTinKhachHang;
            existing.SkVA = dto.SkVA ?? existing.SkVA;
            existing.DienAp = dto.DienAp ?? existing.DienAp;
            existing.NgayNhan = dto.NgayNhan ?? existing.NgayNhan;
            existing.NguoiThucHien = dto.NguoiThucHien ?? existing.NguoiThucHien;
            existing.SoMay = dto.SoMay ?? existing.SoMay;
            existing.SoTBKTSua = dto.SoTBKTSua ?? existing.SoTBKTSua;
            existing.GiaoPKD = dto.GiaoPKD ?? existing.GiaoPKD;
            existing.GhiChu = dto.GhiChu ?? existing.GhiChu;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating MaySuaChua: {Id}", id);
            return StatusCode(500, new { error = "Lỗi cập nhật", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.MaySuaChua.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            _context.MaySuaChua.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting MaySuaChua: {Id}", id);
            return StatusCode(500, new { error = "Lỗi xóa", message = ex.Message });
        }
    }
}
