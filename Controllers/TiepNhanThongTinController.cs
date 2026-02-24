using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TiepNhanThongTinController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TiepNhanThongTinController>? _logger;

    public TiepNhanThongTinController(
        ApplicationDbContext context,
        ILogger<TiepNhanThongTinController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TiepNhanThongTin>>> GetAll()
    {
        try
        {
            var items = await _context.TiepNhanThongTin
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting TiepNhanThongTin list");
            return StatusCode(500, new { error = "Lỗi tải danh sách", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TiepNhanThongTin>> GetById(int id)
    {
        try
        {
            var item = await _context.TiepNhanThongTin.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting TiepNhanThongTin by Id: {Id}", id);
            return StatusCode(500, new { error = "Lỗi tải bản ghi", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TiepNhanThongTin>> Create([FromBody] TiepNhanThongTin dto)
    {
        try
        {
            var entity = new TiepNhanThongTin
            {
                SoTNTT = dto.SoTNTT ?? string.Empty,
                DienAp = dto.DienAp ?? string.Empty,
                SoLuong = dto.SoLuong,
                TieuChuan = dto.TieuChuan,
                PhuKienKemTheo = dto.PhuKienKemTheo,
                KhachHang = dto.KhachHang ?? string.Empty,
                NgayNhan = dto.NgayNhan,
                NgayGiao = dto.NgayGiao,
                NgayLuu = dto.NgayLuu,
                NguoiThucHien = dto.NguoiThucHien,
                NgayHoanThanh = dto.NgayHoanThanh,
                GhiChu = dto.GhiChu
            };
            _context.TiepNhanThongTin.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating TiepNhanThongTin");
            return StatusCode(500, new { error = "Lỗi thêm mới", message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TiepNhanThongTin>> Update(int id, [FromBody] TiepNhanThongTin dto)
    {
        try
        {
            var existing = await _context.TiepNhanThongTin.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });

            existing.SoTNTT = dto.SoTNTT ?? existing.SoTNTT;
            existing.DienAp = dto.DienAp ?? existing.DienAp;
            existing.SoLuong = dto.SoLuong;
            existing.TieuChuan = dto.TieuChuan ?? existing.TieuChuan;
            existing.PhuKienKemTheo = dto.PhuKienKemTheo ?? existing.PhuKienKemTheo;
            existing.KhachHang = dto.KhachHang ?? existing.KhachHang;
            existing.NgayNhan = dto.NgayNhan;
            existing.NgayGiao = dto.NgayGiao;
            existing.NgayLuu = dto.NgayLuu;
            existing.NguoiThucHien = dto.NguoiThucHien ?? existing.NguoiThucHien;
            existing.NgayHoanThanh = dto.NgayHoanThanh ?? existing.NgayHoanThanh;
            existing.GhiChu = dto.GhiChu ?? existing.GhiChu;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating TiepNhanThongTin: {Id}", id);
            return StatusCode(500, new { error = "Lỗi cập nhật", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.TiepNhanThongTin.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            _context.TiepNhanThongTin.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting TiepNhanThongTin: {Id}", id);
            return StatusCode(500, new { error = "Lỗi xóa", message = ex.Message });
        }
    }
}
