using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using quanlyfilesBE.Models;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationsController>? _logger;

    public NotificationsController(
        ApplicationDbContext context,
        ILogger<NotificationsController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/notifications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Lấy notifications không phải WorkItem
            var nonWorkItemNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.RelatedEntityType != "WorkItem")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Lấy notifications của work items chưa hoàn thành (ActualFinish == null) VÀ chưa được xác nhận (PersonConfirmation != true)
            var workItemNotifications = await (from n in _context.Notifications
                                               where n.UserId == userId && n.RelatedEntityType == "WorkItem" && n.RelatedEntityId.HasValue
                                               join wi in _context.WorkItems on n.RelatedEntityId.Value equals wi.WorkItemID
                                               where wi.ActualFinish == null && wi.PersonConfirmation != true
                                               orderby n.CreatedAt descending
                                               select n).ToListAsync();

            // Kết hợp và sắp xếp lại
            var allNotifications = nonWorkItemNotifications.Concat(workItemNotifications)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return Ok(allNotifications);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetNotifications: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving notifications", message = ex.Message });
        }
    }

    // GET: api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger?.LogInformation("GetUnreadCount called with userId: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Đếm notifications không phải WorkItem
            var nonWorkItemCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && n.RelatedEntityType != "WorkItem");

            // Đếm notifications của work items chưa hoàn thành (ActualFinish == null) VÀ chưa được xác nhận (PersonConfirmation != true)
            var workItemNotifications = await (from n in _context.Notifications
                                               where n.UserId == userId && !n.IsRead && n.RelatedEntityType == "WorkItem" && n.RelatedEntityId.HasValue
                                               join wi in _context.WorkItems on n.RelatedEntityId.Value equals wi.WorkItemID
                                               where wi.ActualFinish == null && wi.PersonConfirmation != true
                                               select n).CountAsync();

            var count = nonWorkItemCount + workItemNotifications;

            _logger?.LogInformation("GetUnreadCount: Found {Count} unread notifications for userId: {UserId}", count, userId);

            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetUnreadCount: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving unread count", message = ex.Message });
        }
    }

    // PUT: api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new { error = "Notification not found" });
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in MarkAsRead: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error marking notification as read", message = ex.Message });
        }
    }

    // PUT: api/notifications/mark-all-read
    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in MarkAllAsRead: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error marking all notifications as read", message = ex.Message });
        }
    }

    // POST: api/notifications/sync-my-notifications
    [HttpPost("sync-my-notifications")]
    public async Task<IActionResult> SyncMyNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger?.LogInformation("SyncMyNotifications called with userId: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                _logger?.LogWarning("SyncMyNotifications: User not authenticated");
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Get current user from database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == userId);

            _logger?.LogInformation("SyncMyNotifications: User found - UserId: {UserId}, UserName: {UserName}, FullName: {FullName}", 
                user?.UserId, user?.UserName, user?.FullName);

            if (user == null)
            {
                _logger?.LogWarning("SyncMyNotifications: User not found for FirebaseUID: {FirebaseUID}", userId);
                return NotFound(new { error = "User not found" });
            }

            // Get notification preference
            var notificationSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
            
            var sendEmailNotifications = true; // Default to email
            if (notificationSetting != null)
            {
                bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
            }

            // If email notifications are enabled, don't create badge notifications
            if (sendEmailNotifications)
            {
                return Ok(new { message = "Email notifications are enabled. No badge notifications needed.", created = 0 });
            }

            // Get all work items assigned to this user that don't have notifications yet
            var userIdentifiers = new List<string>
            {
                user.UserId.ToString(),
                user.UserName ?? string.Empty,
                user.FullName ?? string.Empty
            }.Where(id => !string.IsNullOrEmpty(id)).ToList();

            _logger?.LogInformation("SyncMyNotifications: Searching work items with identifiers: {Identifiers}", string.Join(", ", userIdentifiers));

            var workItems = await _context.WorkItems
                .Include(wi => wi.MachineAssignment)
                .Where(wi => !string.IsNullOrEmpty(wi.PersonName) && 
                             userIdentifiers.Any(id => wi.PersonName == id))
                .ToListAsync();

            _logger?.LogInformation("SyncMyNotifications: Found {Count} work items for user", workItems.Count);

            var createdCount = 0;
            var skippedCount = 0;

            foreach (var workItem in workItems)
            {
                _logger?.LogInformation("SyncMyNotifications: Processing work item {WorkItemID}, PersonName: {PersonName}", 
                    workItem.WorkItemID, workItem.PersonName);

                // Skip work items that are already completed or confirmed
                if (workItem.ActualFinish.HasValue || workItem.PersonConfirmation == true)
                {
                    _logger?.LogInformation("SyncMyNotifications: Skipping work item {WorkItemID} - already completed or confirmed", workItem.WorkItemID);
                    skippedCount++;
                    continue;
                }

                // Check if notification already exists
                var existingNotification = await _context.Notifications
                    .FirstOrDefaultAsync(n => 
                        n.UserId == userId &&
                        n.RelatedEntityType == "WorkItem" && 
                        n.RelatedEntityId == workItem.WorkItemID);

                if (existingNotification != null)
                {
                    _logger?.LogInformation("SyncMyNotifications: Notification already exists for work item {WorkItemID}", workItem.WorkItemID);
                    skippedCount++;
                    continue;
                }

                // Get TBKT_ID from assignment
                var assignment = await _context.MachineAssignments
                    .Include(a => a.TechnicalSheet)
                    .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                
                var tbktId = assignment?.TechnicalSheet?.TBKT_ID ?? "N/A";

                // Create notification - chỉ hiển thị TBKT_ID
                var notification = new Notification
                {
                    UserId = userId,
                    Title = "Bạn đã được giao công việc mới",
                    Message = tbktId,
                    Type = "info",
                    IsRead = false,
                    CreatedAt = workItem.StartDate ?? DateTime.UtcNow,
                    RelatedEntityType = "WorkItem",
                    RelatedEntityId = workItem.WorkItemID
                };

                _context.Notifications.Add(notification);
                createdCount++;
                _logger?.LogInformation("SyncMyNotifications: Created notification for work item {WorkItemID}", workItem.WorkItemID);
            }

            await _context.SaveChangesAsync();

            _logger?.LogInformation("SyncMyNotifications: Completed - Created: {Created}, Skipped: {Skipped}", createdCount, skippedCount);

            return Ok(new { 
                message = "Notifications synced successfully",
                created = createdCount,
                skipped = skippedCount
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in SyncMyNotifications: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error syncing notifications", message = ex.Message });
        }
    }

    // POST: api/notifications/create-for-existing-work-items
    [HttpPost("create-for-existing-work-items")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> CreateNotificationsForExistingWorkItems()
    {
        try
        {
            // Get notification preference
            var notificationSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
            
            var sendEmailNotifications = true; // Default to email
            if (notificationSetting != null)
            {
                bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
            }

            // If email notifications are enabled, don't create badge notifications
            if (sendEmailNotifications)
            {
                return Ok(new { message = "Email notifications are enabled. No badge notifications needed." });
            }

            // Get all work items that don't have notifications yet
            var workItems = await _context.WorkItems
                .Include(wi => wi.MachineAssignment)
                .Where(wi => !string.IsNullOrEmpty(wi.PersonName))
                .ToListAsync();

            var createdCount = 0;
            var skippedCount = 0;

            foreach (var workItem in workItems)
            {
                // Check if notification already exists
                var existingNotification = await _context.Notifications
                    .FirstOrDefaultAsync(n => 
                        n.RelatedEntityType == "WorkItem" && 
                        n.RelatedEntityId == workItem.WorkItemID);

                if (existingNotification != null)
                {
                    skippedCount++;
                    continue;
                }

                // Find user by PersonName
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        u.UserName == workItem.PersonName || 
                        u.FullName == workItem.PersonName ||
                        u.UserId.ToString() == workItem.PersonName);

                if (user == null || string.IsNullOrEmpty(user.FirebaseUID))
                {
                    skippedCount++;
                    continue;
                }

                // Get TBKT_ID from assignment
                var assignment = await _context.MachineAssignments
                    .Include(a => a.TechnicalSheet)
                    .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                
                var tbktId = assignment?.TechnicalSheet?.TBKT_ID ?? "N/A";

                // Create notification - chỉ hiển thị TBKT_ID
                var notification = new Notification
                {
                    UserId = user.FirebaseUID,
                    Title = "Bạn đã được giao công việc mới",
                    Message = tbktId,
                    Type = "info",
                    IsRead = false,
                    CreatedAt = workItem.StartDate ?? DateTime.UtcNow,
                    RelatedEntityType = "WorkItem",
                    RelatedEntityId = workItem.WorkItemID
                };

                _context.Notifications.Add(notification);
                createdCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Notifications created successfully",
                created = createdCount,
                skipped = skippedCount
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateNotificationsForExistingWorkItems: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating notifications", message = ex.Message });
        }
    }

    private string? GetCurrentUserId()
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sub = User.FindFirst("sub")?.Value;
        var result = nameIdentifier ?? sub;
        _logger?.LogInformation("GetCurrentUserId: NameIdentifier={NameIdentifier}, Sub={Sub}, Result={Result}", 
            nameIdentifier, sub, result);
        return result;
    }
}
