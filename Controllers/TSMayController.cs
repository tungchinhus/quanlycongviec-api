using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TSMayController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TSMayController>? _logger;

    public TSMayController(
        ApplicationDbContext context,
        ILogger<TSMayController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/tsmay
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TSMay>>> GetAll()
    {
        try
        {
            var items = await _context.TSMay
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all TSMay records");
            return StatusCode(500, new { error = "Error retrieving TSMay records", message = ex.Message });
        }
    }

    // GET: api/tsmay/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TSMay>> GetById(int id)
    {
        try
        {
            var item = await _context.TSMay.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { message = "TSMay not found" });
            }
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting TSMay by ID: {Id}", id);
            return StatusCode(500, new { error = "Error retrieving TSMay record", message = ex.Message });
        }
    }

    // POST: api/tsmay
    [HttpPost]
    public async Task<ActionResult<TSMay>> Create([FromBody] CreateTSMayDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newItem = new TSMay
            {
                CongSuat = dto.CongSuat,
                SoMay = dto.SoMay,
                SBB = dto.SBB,
                LSX = dto.LSX,
                TChuanLSX = dto.TChuanLSX,
                TBKT = dto.TBKT,
                Po = dto.Po,
                Io = dto.Io,
                Pk75H1 = dto.Pk75H1,
                Pk75H2 = dto.Pk75H2,
                Uk75H1 = dto.Uk75H1,
                Uk75H2 = dto.Uk75H2,
                UdmHVH1 = dto.UdmHVH1,
                UdmHVH2 = dto.UdmHVH2,
                UdmLV = dto.UdmLV
            };

            _context.TSMay.Add(newItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newItem.Id }, newItem);
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database error creating TSMay record");
            return StatusCode(500, new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating TSMay record");
            return StatusCode(500, new { error = "Error creating TSMay record", message = ex.Message });
        }
    }

    // POST: api/tsmay/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateTSMayResponseDto>> BulkCreate([FromBody] BulkCreateTSMayDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = new List<TSMay>();
            var errors = new List<BulkCreateErrorDto>();

            // Use transaction for bulk insert - all or nothing
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < dto.Items.Count; i++)
                {
                    try
                    {
                        var itemDto = dto.Items[i];
                        var newItem = new TSMay
                        {
                            CongSuat = itemDto.CongSuat,
                            SoMay = itemDto.SoMay,
                            SBB = itemDto.SBB,
                            LSX = itemDto.LSX,
                            TChuanLSX = itemDto.TChuanLSX,
                            TBKT = itemDto.TBKT,
                            Po = itemDto.Po,
                            Io = itemDto.Io,
                            Pk75H1 = itemDto.Pk75H1,
                            Pk75H2 = itemDto.Pk75H2,
                            Uk75H1 = itemDto.Uk75H1,
                            Uk75H2 = itemDto.Uk75H2,
                            UdmHVH1 = itemDto.UdmHVH1,
                            UdmHVH2 = itemDto.UdmHVH2,
                            UdmLV = itemDto.UdmLV
                        };

                        _context.TSMay.Add(newItem);
                        await _context.SaveChangesAsync();
                        created.Add(newItem);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        errors.Add(new BulkCreateErrorDto
                        {
                            Index = i,
                            Data = dto.Items[i],
                            Error = dbEx.InnerException?.Message ?? dbEx.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BulkCreateErrorDto
                        {
                            Index = i,
                            Data = dto.Items[i],
                            Error = ex.Message
                        });
                    }
                }

                if (errors.Count == 0)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                    created.Clear();
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error in bulk create transaction");
                throw;
            }

            var response = new BulkCreateTSMayResponseDto
            {
                Success = errors.Count == 0,
                Total = dto.Items.Count,
                Created = created.Count,
                Failed = errors.Count,
                Errors = errors.Count > 0 ? errors : null
            };

            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error bulk creating TSMay records");
            return StatusCode(500, new { error = "Error bulk creating TSMay records", message = ex.Message });
        }
    }

    // PUT: api/tsmay/5
    [HttpPut("{id}")]
    public async Task<ActionResult<TSMay>> Update(int id, [FromBody] UpdateTSMayDto dto)
    {
        try
        {
            var existingItem = await _context.TSMay.FindAsync(id);
            if (existingItem == null)
            {
                return NotFound(new { message = "TSMay not found" });
            }

            // Update only provided fields
            if (dto.CongSuat.HasValue)
                existingItem.CongSuat = dto.CongSuat;
            
            if (dto.SoMay != null)
                existingItem.SoMay = dto.SoMay;
            
            if (dto.SBB != null)
                existingItem.SBB = dto.SBB;
            
            if (dto.LSX != null)
                existingItem.LSX = dto.LSX;
            
            if (dto.TChuanLSX != null)
                existingItem.TChuanLSX = dto.TChuanLSX;
            
            if (dto.TBKT != null)
                existingItem.TBKT = dto.TBKT;
            
            if (dto.Po != null)
                existingItem.Po = dto.Po;
            
            if (dto.Io != null)
                existingItem.Io = dto.Io;
            
            if (dto.Pk75H1 != null)
                existingItem.Pk75H1 = dto.Pk75H1;
            
            if (dto.Pk75H2 != null)
                existingItem.Pk75H2 = dto.Pk75H2;
            
            if (dto.Uk75H1 != null)
                existingItem.Uk75H1 = dto.Uk75H1;
            
            if (dto.Uk75H2 != null)
                existingItem.Uk75H2 = dto.Uk75H2;
            
            if (dto.UdmHVH1 != null)
                existingItem.UdmHVH1 = dto.UdmHVH1;
            
            if (dto.UdmHVH2 != null)
                existingItem.UdmHVH2 = dto.UdmHVH2;
            
            if (dto.UdmLV != null)
                existingItem.UdmLV = dto.UdmLV;

            await _context.SaveChangesAsync();

            return Ok(existingItem);
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database error updating TSMay record: {Id}", id);
            return StatusCode(500, new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating TSMay record: {Id}", id);
            return StatusCode(500, new { error = "Error updating TSMay record", message = ex.Message });
        }
    }

    // DELETE: api/tsmay/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.TSMay.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { message = "TSMay not found" });
            }

            _context.TSMay.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting TSMay record: {Id}", id);
            return StatusCode(500, new { error = "Error deleting TSMay record", message = ex.Message });
        }
    }

    // GET: api/tsmay/search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TSMay>>> Search(
        [FromQuery] string? soMay = null,
        [FromQuery] string? sbb = null,
        [FromQuery] string? lsx = null,
        [FromQuery] int? congSuat = null)
    {
        try
        {
            var query = _context.TSMay.AsQueryable();

            if (!string.IsNullOrEmpty(soMay))
                query = query.Where(x => x.SoMay == soMay);
            
            if (!string.IsNullOrEmpty(sbb))
                query = query.Where(x => x.SBB == sbb);
            
            if (!string.IsNullOrEmpty(lsx))
                query = query.Where(x => x.LSX == lsx);
            
            if (congSuat.HasValue)
                query = query.Where(x => x.CongSuat == congSuat.Value);

            var results = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching TSMay records");
            return StatusCode(500, new { error = "Error searching TSMay records", message = ex.Message });
        }
    }
}

