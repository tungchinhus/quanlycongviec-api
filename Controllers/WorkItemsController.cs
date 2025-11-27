using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkItemsController>? _logger;

    public WorkItemsController(ApplicationDbContext context, ILogger<WorkItemsController>? logger = null)
    {
        _context = context;
        _logger = logger;
        _logger?.LogInformation("WorkItemsController initialized");
    }

    // Test endpoint để kiểm tra controller có hoạt động không
    [HttpGet("test")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Tạm thời cho phép không cần auth để test
    public IActionResult Test()
    {
        _logger?.LogInformation("Test endpoint called");
        return Ok(new { message = "WorkItemsController is working", timestamp = DateTime.UtcNow });
    }

    // GET: api/work-items
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkItemDto>>> GetWorkItems([FromQuery] int? assignmentID = null)
    {
        try
        {
            IQueryable<WorkItem> query = _context.WorkItems;

            if (assignmentID.HasValue)
            {
                query = query.Where(wi => wi.AssignmentID == assignmentID.Value);
            }

            var workItems = await query.ToListAsync();

            var workItemDtos = workItems.Select(wi => new WorkItemDto
            {
                WorkItemID = wi.WorkItemID,
                AssignmentID = wi.AssignmentID,
                WorkType = wi.WorkType,
                PersonName = wi.PersonName,
                StartDate = wi.StartDate,
                ExpectedFinish = wi.ExpectedFinish,
                ActualFinish = wi.ActualFinish,
                PersonConfirmation = wi.PersonConfirmation,
                Notes = wi.Notes
            });

            return Ok(workItemDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWorkItems: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error getting work items", message = ex.Message });
        }
    }

    // GET: api/work-items/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkItemDto>> GetWorkItem(int id)
    {
        try
        {
            _logger?.LogInformation("GetWorkItem called with ID: {WorkItemID}", id);
            
            // Sử dụng FirstOrDefaultAsync thay vì FindAsync để đảm bảo query từ database
            var workItem = await _context.WorkItems
                .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
            
            if (workItem == null)
            {
                _logger?.LogWarning("GetWorkItem: Work item with ID {WorkItemID} not found", id);
                return NotFound(new { error = "Work item not found", workItemID = id });
            }
            
            _logger?.LogInformation("GetWorkItem: Found work item {WorkItemID}", id);

            var workItemDto = new WorkItemDto
            {
                WorkItemID = workItem.WorkItemID,
                AssignmentID = workItem.AssignmentID,
                WorkType = workItem.WorkType,
                PersonName = workItem.PersonName,
                StartDate = workItem.StartDate,
                ExpectedFinish = workItem.ExpectedFinish,
                ActualFinish = workItem.ActualFinish,
                PersonConfirmation = workItem.PersonConfirmation,
                Notes = workItem.Notes
            };

            return Ok(workItemDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error getting work item", message = ex.Message });
        }
    }

    // POST: api/work-items
    [HttpPost]
    public async Task<ActionResult<WorkItemDto>> CreateWorkItem([FromBody] CreateWorkItemDto dto)
    {
        try
        {
            // Validate assignment exists
            var assignment = await _context.MachineAssignments.FindAsync(dto.AssignmentID);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            var workItem = new WorkItem
            {
                AssignmentID = dto.AssignmentID,
                WorkType = dto.WorkType,
                PersonName = dto.PersonName,
                StartDate = dto.StartDate,
                ExpectedFinish = dto.ExpectedFinish,
                ActualFinish = dto.ActualFinish,
                PersonConfirmation = dto.PersonConfirmation,
                Notes = dto.Notes
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            var workItemDto = new WorkItemDto
            {
                WorkItemID = workItem.WorkItemID,
                AssignmentID = workItem.AssignmentID,
                WorkType = workItem.WorkType,
                PersonName = workItem.PersonName,
                StartDate = workItem.StartDate,
                ExpectedFinish = workItem.ExpectedFinish,
                ActualFinish = workItem.ActualFinish,
                PersonConfirmation = workItem.PersonConfirmation,
                Notes = workItem.Notes
            };

            return CreatedAtAction(nameof(GetWorkItem), new { id = workItem.WorkItemID }, workItemDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating work item", message = ex.Message });
        }
    }

    // PUT: api/work-items/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkItem(int id, [FromBody] UpdateWorkItemDto dto)
    {
        try
        {
            _logger?.LogInformation("UpdateWorkItem called with ID: {WorkItemID}, DTO: {@Dto}", id, dto);
            
            // Kiểm tra xem có work item nào trong database không
            var totalCount = await _context.WorkItems.CountAsync();
            _logger?.LogInformation("Total work items in database: {Count}", totalCount);
            
            // Lấy tất cả work item IDs để debug
            var allIds = await _context.WorkItems.Select(wi => wi.WorkItemID).ToListAsync();
            _logger?.LogInformation("All work item IDs in database: {@Ids}", allIds);
            
            // Sử dụng FirstOrDefaultAsync thay vì FindAsync để đảm bảo query từ database
            var workItem = await _context.WorkItems
                .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
            
            _logger?.LogInformation("Query result for ID {WorkItemID}: {Found}", id, workItem != null);
            
            if (workItem == null)
            {
                _logger?.LogWarning("Work item with ID {WorkItemID} not found. Available IDs: {@Ids}", id, allIds);
                return NotFound(new { error = "Work item not found", workItemID = id, availableIds = allIds });
            }

            // Update only provided fields
            if (dto.WorkType != null)
                workItem.WorkType = dto.WorkType;

            if (dto.PersonName != null)
                workItem.PersonName = dto.PersonName;

            if (dto.StartDate.HasValue)
                workItem.StartDate = dto.StartDate;

            if (dto.ExpectedFinish.HasValue)
                workItem.ExpectedFinish = dto.ExpectedFinish;

            if (dto.ActualFinish.HasValue)
                workItem.ActualFinish = dto.ActualFinish;

            if (dto.PersonConfirmation.HasValue)
                workItem.PersonConfirmation = dto.PersonConfirmation;

            if (dto.Notes != null)
                workItem.Notes = dto.Notes;

            try
            {
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Successfully updated work item {WorkItemID}", id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error updating work item {WorkItemID}: {Message}", id, dbEx.Message);
                return StatusCode(500, new { error = "Error updating work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception saveEx)
            {
                _logger?.LogError(saveEx, "Error saving work item {WorkItemID}: {Message}", id, saveEx.Message);
                return StatusCode(500, new { error = "Error updating work item", message = saveEx.Message });
            }

            var workItemDto = new WorkItemDto
            {
                WorkItemID = workItem.WorkItemID,
                AssignmentID = workItem.AssignmentID,
                WorkType = workItem.WorkType,
                PersonName = workItem.PersonName,
                StartDate = workItem.StartDate,
                ExpectedFinish = workItem.ExpectedFinish,
                ActualFinish = workItem.ActualFinish,
                PersonConfirmation = workItem.PersonConfirmation,
                Notes = workItem.Notes
            };

            return Ok(workItemDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating work item", message = ex.Message });
        }
    }

    // DELETE: api/work-items/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkItem(int id)
    {
        try
        {
            // Sử dụng FirstOrDefaultAsync thay vì FindAsync để đảm bảo query từ database
            var workItem = await _context.WorkItems
                .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
            
            if (workItem == null)
            {
                return NotFound(new { error = "Work item not found", workItemID = id });
            }

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting work item", message = ex.Message });
        }
    }
}


