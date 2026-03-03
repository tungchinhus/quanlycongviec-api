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
    public async Task<ActionResult<IEnumerable<TiepNhanThongTin>>> GetAll([FromQuery] string? search = null)
    {
        try
        {
            var query = _context.TiepNhanThongTin.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.SoTNTT != null && x.SoTNTT.ToLower().Contains(term)) ||
                    (x.DienAp != null && x.DienAp.ToLower().Contains(term)) ||
                    (x.KhachHang != null && x.KhachHang.ToLower().Contains(term)) ||
                    (x.TieuChuan != null && x.TieuChuan.ToLower().Contains(term)) ||
                    (x.PhuKienKemTheo != null && x.PhuKienKemTheo.ToLower().Contains(term)) ||
                    (x.NguoiThucHien != null && x.NguoiThucHien.ToLower().Contains(term)) ||
                    (x.GhiChu != null && x.GhiChu.ToLower().Contains(term)) ||
                    (x.ThangNam != null && x.ThangNam.ToLower().Contains(term)) ||
                    (x.TenNVPKD != null && x.TenNVPKD.ToLower().Contains(term)) ||
                    (x.SkVA != null && x.SkVA.ToLower().Contains(term)) ||
                    (x.PhanLoai != null && x.PhanLoai.ToLower().Contains(term)));
            }
            var items = await query
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
                ThangNam = dto.ThangNam,
                TenNVPKD = dto.TenNVPKD,
                SkVA = dto.SkVA,
                NgayNhan = dto.NgayNhan,
                NgayGiao = dto.NgayGiao,
                NgayLuu = dto.NgayLuu,
                NguoiThucHien = dto.NguoiThucHien,
                NgayHoanThanh = dto.NgayHoanThanh,
                GhiChu = dto.GhiChu,
                PhanLoai = dto.PhanLoai
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
            existing.ThangNam = dto.ThangNam ?? existing.ThangNam;
            existing.TenNVPKD = dto.TenNVPKD ?? existing.TenNVPKD;
            existing.SkVA = dto.SkVA ?? existing.SkVA;
            existing.NgayNhan = dto.NgayNhan;
            existing.NgayGiao = dto.NgayGiao;
            existing.NgayLuu = dto.NgayLuu;
            existing.NguoiThucHien = dto.NguoiThucHien ?? existing.NguoiThucHien;
            existing.NgayHoanThanh = dto.NgayHoanThanh ?? existing.NgayHoanThanh;
            existing.GhiChu = dto.GhiChu ?? existing.GhiChu;
            existing.PhanLoai = dto.PhanLoai ?? existing.PhanLoai;

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
