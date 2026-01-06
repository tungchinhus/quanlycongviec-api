using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Services;
using quanlyfilesBE.Hubs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/work-items")]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPowerAutomateService? _powerAutomateService;
    private readonly IHubContext<NotificationHub>? _hubContext;
    private readonly ILogger<WorkItemsController>? _logger;

    public WorkItemsController(
        ApplicationDbContext context, 
        IPowerAutomateService? powerAutomateService = null,
        IHubContext<NotificationHub>? hubContext = null,
        ILogger<WorkItemsController>? logger = null)
    {
        _context = context;
        _powerAutomateService = powerAutomateService;
        _hubContext = hubContext;
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
                Notes = wi.Notes,
                File_ID = wi.File_ID
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
                Notes = workItem.Notes,
                File_ID = workItem.File_ID
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
        // Use execution strategy to support retries with transaction
        WorkItem? workItem = null;
        MachineAssignment? assignment = null;
        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate assignment exists
                    assignment = await _context.MachineAssignments.FindAsync(dto.AssignmentID);
                    if (assignment == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    workItem = new WorkItem
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
                    
                    // Khi user tạo work item với xác nhận (PersonConfirmation = true), cập nhật trạng thái giao việc thành 2 (đang xử lý)
                    if (dto.PersonConfirmation == true && assignment.Status == 1)
                    {
                        assignment.Status = 2; // 2: đang xử lý
                        _logger?.LogInformation("Updated MachineAssignment {AssignmentID} status from 1 to 2 after creating work item with confirmation", assignment.AssignmentID);
                    }
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(dbEx, "Database error creating work item: {Message}", dbEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(ex, "Error in CreateWorkItem transaction: {Message}", ex.Message);
                    throw;
                }
            });

            // Check if assignment was found
            if (workItem == null || assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            // Handle notification for assigned user
            try
            {
                await HandleWorkItemAssignmentNotificationAsync(workItem, assignment);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the work item creation if notification fails
                _logger?.LogWarning(notifEx, "Failed to send notification for work item assignment: {Message}", notifEx.Message);
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

            return CreatedAtAction(nameof(GetWorkItem), new { id = workItem.WorkItemID }, workItemDto);
        }
        catch (DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error creating work item: {Message}", dbEx.Message);
            return StatusCode(500, new { error = "Error creating work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating work item", message = ex.Message });
        }
    }

    // PUT: api/work-items/{id}
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateWorkItem(int id, [FromBody] UpdateWorkItemDto? dto)
    {
        _logger?.LogInformation("UpdateWorkItem called with ID: {WorkItemID}, DTO: {@Dto}", id, dto);
        
        // Validate DTO
        if (dto == null)
        {
            _logger?.LogWarning("UpdateWorkItem: DTO is null for ID {WorkItemID}", id);
            return BadRequest(new { error = "Request body is required" });
        }

        // Use execution strategy to support retries with transaction
        WorkItem? workItem = null;
        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get work item from database
                    workItem = await _context.WorkItems
                        .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
                    
                    if (workItem == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }
                    
                    _logger?.LogInformation("Found work item {WorkItemID} for update", id);

                    // Update only provided fields
                    if (dto.WorkType != null)
                    {
                        workItem.WorkType = dto.WorkType;
                    }

                    if (dto.PersonName != null)
                    {
                        workItem.PersonName = dto.PersonName;
                    }

                    if (dto.StartDate.HasValue)
                    {
                        workItem.StartDate = dto.StartDate.Value;
                    }

                    if (dto.ExpectedFinish.HasValue)
                    {
                        workItem.ExpectedFinish = dto.ExpectedFinish.Value;
                    }

                    if (dto.ActualFinish.HasValue)
                    {
                        workItem.ActualFinish = dto.ActualFinish.Value;
                    }

                    if (dto.PersonConfirmation.HasValue)
                    {
                        workItem.PersonConfirmation = dto.PersonConfirmation.Value;
                        
                        // Khi user xác nhận (PersonConfirmation = true), cập nhật trạng thái giao việc thành 2 (đang xử lý)
                        if (dto.PersonConfirmation.Value == true)
                        {
                            var assignment = await _context.MachineAssignments
                                .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                            
                            if (assignment != null && assignment.Status == 1)
                            {
                                assignment.Status = 2; // 2: đang xử lý
                                _logger?.LogInformation("Updated MachineAssignment {AssignmentID} status from 1 to 2 after confirmation", assignment.AssignmentID);
                            }
                        }
                    }

                    if (dto.Notes != null)
                    {
                        workItem.Notes = dto.Notes;
                    }

                    // Save changes first to get updated work item state
                    await _context.SaveChangesAsync();

                    // Xóa notification nếu work item đã hoàn thành (ActualFinish != null) hoặc đã được xác nhận (PersonConfirmation = true)
                    bool shouldDeleteNotification = workItem.ActualFinish.HasValue || workItem.PersonConfirmation == true;
                    
                    if (shouldDeleteNotification)
                    {
                        var notifications = await _context.Notifications
                            .Where(n => 
                                n.RelatedEntityType == "WorkItem" && 
                                n.RelatedEntityId == workItem.WorkItemID)
                            .ToListAsync();
                        
                        if (notifications.Any())
                        {
                            var userIds = notifications.Select(n => n.UserId).Distinct().ToList();
                            _context.Notifications.RemoveRange(notifications);
                            
                            string reason = workItem.ActualFinish.HasValue 
                                ? "completed" 
                                : "confirmed";
                            _logger?.LogInformation("Deleted {Count} notification(s) because work item {WorkItemID} is {Reason}", notifications.Count, workItem.WorkItemID, reason);
                            
                            // Gửi SignalR notification để client update
                            if (_hubContext != null)
                            {
                                foreach (var userId in userIds)
                                {
                                    await _hubContext.Clients.Group($"user_{userId}").SendAsync("NotificationRemoved", new { workItemId = workItem.WorkItemID });
                                    await _hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCountChanged");
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger?.LogInformation("Successfully updated work item {WorkItemID}", id);
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(dbEx, "Database error updating work item {WorkItemID}: {Message}", id, dbEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(ex, "Error in UpdateWorkItem transaction: {Message}", ex.Message);
                    throw;
                }
            });

            // Check if work item was found
            if (workItem == null)
            {
                return NotFound(new { error = "Work item not found", workItemID = id });
            }

            // Return updated work item
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
                Notes = workItem.Notes,
                File_ID = workItem.File_ID
            };

            return Ok(workItemDto);
        }
        catch (DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error updating work item {WorkItemID}: {Message}", id, dbEx.Message);
            return StatusCode(500, new { error = "Error updating work item", message = dbEx.InnerException?.Message ?? dbEx.Message, details = dbEx.ToString() });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating work item", message = ex.Message, details = ex.ToString() });
        }
    }

    // DELETE: api/work-items/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkItem(int id)
    {
        // Check if work item exists first
        var workItemExists = await _context.WorkItems
            .AnyAsync(wi => wi.WorkItemID == id);
        
        if (!workItemExists)
        {
            return NotFound(new { error = "Work item not found", workItemID = id });
        }

        // Use execution strategy to support retries with transaction
        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Sử dụng FirstOrDefaultAsync thay vì FindAsync để đảm bảo query từ database
                    var workItem = await _context.WorkItems
                        .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
                    
                    if (workItem == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    _context.WorkItems.Remove(workItem);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(dbEx, "Database error deleting work item {WorkItemID}: {Message}", id, dbEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(ex, "Error in DeleteWorkItem transaction: {Message}", ex.Message);
                    throw;
                }
            });

            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error deleting work item {WorkItemID}: {Message}", id, dbEx.Message);
            return StatusCode(500, new { error = "Error deleting work item", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting work item", message = ex.Message });
        }
    }

    private async Task HandleWorkItemAssignmentNotificationAsync(WorkItem workItem, MachineAssignment assignment)
    {
        if (string.IsNullOrWhiteSpace(workItem.PersonName))
        {
            _logger?.LogWarning("WorkItem {WorkItemID} has no PersonName, skipping notification", workItem.WorkItemID);
            return;
        }

        // Find user by PersonName (could be UserName, FullName, or UserId)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.UserName == workItem.PersonName || 
                u.FullName == workItem.PersonName ||
                u.UserId.ToString() == workItem.PersonName);

        if (user == null || string.IsNullOrEmpty(user.FirebaseUID))
        {
            _logger?.LogWarning("User not found for PersonName: {PersonName}, skipping notification", workItem.PersonName);
            return;
        }

        // Get notification preference from settings
        var notificationSetting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
        
        var sendEmailNotifications = true; // Default to email
        if (notificationSetting != null)
        {
            bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
        }

        if (sendEmailNotifications)
        {
            // Send email notification
            if (_powerAutomateService != null && !string.IsNullOrEmpty(user.Email))
            {
                var subject = $"Bạn đã được giao công việc mới: {assignment.MachineName}";
                var body = $@"
                    <h2>Bạn đã được giao công việc mới</h2>
                    <p><strong>Tên máy:</strong> {assignment.MachineName}</p>
                    <p><strong>Loại công việc:</strong> {workItem.WorkType}</p>
                    <p><strong>Ngày bắt đầu:</strong> {workItem.StartDate:dd/MM/yyyy}</p>
                    <p><strong>Dự kiến hoàn thành:</strong> {workItem.ExpectedFinish:dd/MM/yyyy}</p>
                    {(string.IsNullOrEmpty(workItem.Notes) ? "" : $"<p><strong>Ghi chú:</strong> {workItem.Notes}</p>")}
                ";

                // Create a minimal ApprovalWorkflowDto for email
                var workflowDto = new ApprovalWorkflowDto
                {
                    WorkflowID = 0,
                    RequestTitle = $"Công việc mới: {assignment.MachineName}",
                    RequesterName = workItem.PersonName,
                    RequesterEmail = user.Email
                };

                await _powerAutomateService.SendNotificationEmailAsync(
                    user.Email,
                    subject,
                    body,
                    workflowDto);
            }
        }
        else
        {
            // Get TBKT_ID from assignment
            var assignmentWithTBKT = await _context.MachineAssignments
                .Include(a => a.TechnicalSheet)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);
            
            var tbktId = assignmentWithTBKT?.TechnicalSheet?.TBKT_ID ?? "N/A";
            
            // Create notification badge - chỉ hiển thị TBKT_ID
            var notification = new Notification
            {
                UserId = user.FirebaseUID,
                Title = "Bạn đã được giao công việc mới",
                Message = tbktId,
                Type = "info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityType = "WorkItem",
                RelatedEntityId = workItem.WorkItemID
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            _logger?.LogInformation("Created notification for user {FirebaseUID} about work item {WorkItemID}", user.FirebaseUID, workItem.WorkItemID);
            
            // Gửi SignalR notification
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"user_{user.FirebaseUID}").SendAsync("NewNotification", new 
                { 
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    createdAt = notification.CreatedAt
                });
                await _hubContext.Clients.Group($"user_{user.FirebaseUID}").SendAsync("UnreadCountChanged");
            }
        }
    }
}

