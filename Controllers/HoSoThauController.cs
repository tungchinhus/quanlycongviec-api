using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HoSoThauController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HoSoThauController>? _logger;

    public HoSoThauController(
        ApplicationDbContext context,
        ILogger<HoSoThauController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HoSoThau>>> GetAll([FromQuery] string? search = null)
    {
        try
        {
            var query = _context.HoSoThau.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.SoHST != null && x.SoHST.ToLower().Contains(term)) ||
                    (x.DonViMoiThau != null && x.DonViMoiThau.ToLower().Contains(term)) ||
                    (x.SoTBMTIB != null && x.SoTBMTIB.ToLower().Contains(term)) ||
                    (x.GhiChu != null && x.GhiChu.ToLower().Contains(term)));
            }
            var items = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting HoSoThau list");
            return StatusCode(500, new { error = "Lỗi tải danh sách", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HoSoThau>> GetById(int id)
    {
        try
        {
            var item = await _context.HoSoThau.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting HoSoThau by Id: {Id}", id);
            return StatusCode(500, new { error = "Lỗi tải bản ghi", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<HoSoThau>> Create([FromBody] HoSoThau dto)
    {
        try
        {
            var entity = new HoSoThau
            {
                SoHST = dto.SoHST ?? string.Empty,
                DonViMoiThau = dto.DonViMoiThau ?? string.Empty,
                SoTBMTIB = dto.SoTBMTIB,
                NgayNhan = dto.NgayNhan,
                NgayGiaoPhongKD = dto.NgayGiaoPhongKD,
                GhiChu = dto.GhiChu
            };
            _context.HoSoThau.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating HoSoThau");
            return StatusCode(500, new { error = "Lỗi thêm mới", message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<HoSoThau>> Update(int id, [FromBody] HoSoThau dto)
    {
        try
        {
            var existing = await _context.HoSoThau.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });

            existing.SoHST = dto.SoHST ?? existing.SoHST;
            existing.DonViMoiThau = dto.DonViMoiThau ?? existing.DonViMoiThau;
            existing.SoTBMTIB = dto.SoTBMTIB ?? existing.SoTBMTIB;
            existing.NgayNhan = dto.NgayNhan;
            existing.NgayGiaoPhongKD = dto.NgayGiaoPhongKD ?? existing.NgayGiaoPhongKD;
            existing.GhiChu = dto.GhiChu ?? existing.GhiChu;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating HoSoThau: {Id}", id);
            return StatusCode(500, new { error = "Lỗi cập nhật", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.HoSoThau.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản ghi" });
            _context.HoSoThau.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting HoSoThau: {Id}", id);
            return StatusCode(500, new { error = "Lỗi xóa", message = ex.Message });
        }
    }
}
