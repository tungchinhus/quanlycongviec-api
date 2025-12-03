using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/work-items")]
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
        // Use transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate assignment exists
            var assignment = await _context.MachineAssignments.FindAsync(dto.AssignmentID);
            if (assignment == null)
            {
                await transaction.RollbackAsync();
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
            await transaction.CommitAsync();

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
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(dbEx, "Database error creating work item: {Message}", dbEx.Message);
            return StatusCode(500, new { error = "Error creating work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error in CreateWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating work item", message = ex.Message });
        }
    }

    // PUT: api/work-items/{id}
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateWorkItem(int id, [FromBody] UpdateWorkItemDto? dto)
    {
        try
        {
            _logger?.LogInformation("UpdateWorkItem called with ID: {WorkItemID}, DTO: {@Dto}", id, dto);
            
            // Validate DTO
            if (dto == null)
            {
                _logger?.LogWarning("UpdateWorkItem: DTO is null for ID {WorkItemID}", id);
                return BadRequest(new { error = "Request body is required" });
            }
            
            // Kiểm tra xem có work item nào trong database không
            var totalCount = await _context.WorkItems.CountAsync();
            _logger?.LogInformation("Total work items in database: {Count}", totalCount);
            
            // Lấy tất cả work item IDs để debug
            var allIds = await _context.WorkItems.Select(wi => wi.WorkItemID).ToListAsync();
            _logger?.LogInformation("All work item IDs in database: {@Ids}", allIds);
            
            // Query work item - select only fields we need to avoid PersonConfirmation cast error
            // Use raw SQL to get PersonConfirmation as string, then convert
            var workItemExists = await _context.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) FROM WorkItem WHERE WorkItemID = {0}", id).FirstOrDefaultAsync();
            
            if (workItemExists == 0)
            {
                _logger?.LogWarning("Work item with ID {WorkItemID} not found. Available IDs: {@Ids}", id, allIds);
                return NotFound(new { error = "Work item not found", workItemID = id, availableIds = allIds });
            }
            
            // Get work item using raw SQL to avoid PersonConfirmation cast error
            // Select all fields except PersonConfirmation, then handle it separately
            var workItemData = await _context.Database.SqlQueryRaw<WorkItemUpdateData>(
                @"SELECT 
                    WorkItemID,
                    AssignmentID,
                    WorkType,
                    PersonName,
                    StartDate,
                    ExpectedFinish,
                    ActualFinish,
                    CAST(PersonConfirmation AS NVARCHAR(10)) AS PersonConfirmationRaw,
                    Notes
                  FROM WorkItem
                  WHERE WorkItemID = {0}",
                id).FirstOrDefaultAsync();
            
            if (workItemData == null)
            {
                _logger?.LogWarning("Work item data not found for ID {WorkItemID}", id);
                return NotFound(new { error = "Work item not found", workItemID = id });
            }
            
            _logger?.LogInformation("Query result for ID {WorkItemID}: Found", id);

            // Use transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Build UPDATE SQL dynamically based on provided fields
                var updateFields = new List<string>();
                var parameters = new List<object>();
                int paramIndex = 0;

                if (dto.WorkType != null)
                {
                    updateFields.Add($"WorkType = {{{paramIndex}}}");
                    parameters.Add(dto.WorkType);
                    paramIndex++;
                }

                if (dto.PersonName != null)
                {
                    updateFields.Add($"PersonName = {{{paramIndex}}}");
                    parameters.Add(dto.PersonName);
                    paramIndex++;
                }

                if (dto.StartDate.HasValue)
                {
                    updateFields.Add($"StartDate = {{{paramIndex}}}");
                    parameters.Add(dto.StartDate.Value);
                    paramIndex++;
                }

                if (dto.ExpectedFinish.HasValue)
                {
                    updateFields.Add($"ExpectedFinish = {{{paramIndex}}}");
                    parameters.Add(dto.ExpectedFinish.Value);
                    paramIndex++;
                }

                if (dto.ActualFinish.HasValue)
                {
                    updateFields.Add($"ActualFinish = {{{paramIndex}}}");
                    parameters.Add(dto.ActualFinish.Value);
                    paramIndex++;
                }

                if (dto.PersonConfirmation.HasValue)
                {
                    updateFields.Add($"PersonConfirmation = {{{paramIndex}}}");
                    parameters.Add(dto.PersonConfirmation.Value ? 1 : 0);
                    paramIndex++;
                }

                if (dto.Notes != null)
                {
                    updateFields.Add($"Notes = {{{paramIndex}}}");
                    parameters.Add(dto.Notes);
                    paramIndex++;
                }

                if (updateFields.Count > 0)
                {
                    // Add WorkItemID parameter
                    parameters.Add(id);
                    
                    var updateSql = $"UPDATE WorkItem SET {string.Join(", ", updateFields)} WHERE WorkItemID = {{{paramIndex}}}";
                    await _context.Database.ExecuteSqlRawAsync(updateSql, parameters.ToArray());
                    _logger?.LogInformation("Updated {FieldCount} fields for WorkItem {WorkItemID}", updateFields.Count, id);
                }
                else
                {
                    _logger?.LogWarning("No fields to update for WorkItem {WorkItemID}", id);
                }

                await transaction.CommitAsync();
                _logger?.LogInformation("Successfully updated work item {WorkItemID}", id);
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(dbEx, "Database error updating work item {WorkItemID}: {Message}", id, dbEx.Message);
                return StatusCode(500, new { error = "Error updating work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception saveEx)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(saveEx, "Error saving work item {WorkItemID}: {Message}", id, saveEx.Message);
                return StatusCode(500, new { error = "Error updating work item", message = saveEx.Message });
            }

            // Get updated work item data using raw SQL to avoid PersonConfirmation cast error
            var updatedWorkItemData = await _context.Database.SqlQueryRaw<WorkItemUpdateData>(
                @"SELECT 
                    WorkItemID,
                    AssignmentID,
                    WorkType,
                    PersonName,
                    StartDate,
                    ExpectedFinish,
                    ActualFinish,
                    CAST(PersonConfirmation AS NVARCHAR(10)) AS PersonConfirmationRaw,
                    Notes
                  FROM WorkItem
                  WHERE WorkItemID = {0}",
                id).FirstOrDefaultAsync();

            if (updatedWorkItemData == null)
            {
                _logger?.LogWarning("Could not retrieve updated work item data for ID {WorkItemID}", id);
                return StatusCode(500, new { error = "Error retrieving updated work item" });
            }

            // Convert PersonConfirmation from string to bool
            bool? personConfirmation = null;
            if (!string.IsNullOrWhiteSpace(updatedWorkItemData.PersonConfirmationRaw))
            {
                var trimmed = updatedWorkItemData.PersonConfirmationRaw.Trim();
                if (bool.TryParse(trimmed, out bool boolValue))
                    personConfirmation = boolValue;
                else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                         trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
                    personConfirmation = true;
                else if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || 
                         trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
                    personConfirmation = false;
            }

            var workItemDto = new WorkItemDto
            {
                WorkItemID = updatedWorkItemData.WorkItemID,
                AssignmentID = updatedWorkItemData.AssignmentID,
                WorkType = updatedWorkItemData.WorkType,
                PersonName = updatedWorkItemData.PersonName,
                StartDate = updatedWorkItemData.StartDate,
                ExpectedFinish = updatedWorkItemData.ExpectedFinish,
                ActualFinish = updatedWorkItemData.ActualFinish,
                PersonConfirmation = personConfirmation,
                Notes = updatedWorkItemData.Notes
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
        // Use transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Sử dụng FirstOrDefaultAsync thay vì FindAsync để đảm bảo query từ database
            var workItem = await _context.WorkItems
                .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
            
            if (workItem == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { error = "Work item not found", workItemID = id });
            }

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(dbEx, "Database error deleting work item {WorkItemID}: {Message}", id, dbEx.Message);
            return StatusCode(500, new { error = "Error deleting work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error in DeleteWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting work item", message = ex.Message });
        }
    }
}

// Helper class for raw SQL query result to handle PersonConfirmation type conversion
internal class WorkItemUpdateData
{
    public int WorkItemID { get; set; }
    public int AssignmentID { get; set; }
    public string? WorkType { get; set; }
    public string? PersonName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedFinish { get; set; }
    public DateTime? ActualFinish { get; set; }
    public string? PersonConfirmationRaw { get; set; }
    public string? Notes { get; set; }
}

