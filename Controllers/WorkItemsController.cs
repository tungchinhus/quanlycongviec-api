using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
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

    // GET: api/work-items/by-date-range
    [HttpGet("by-date-range")]
    public async Task<ActionResult<IEnumerable<WorkItemDto>>> GetWorkItemsByDateRange(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string[]? workTypes = null,
        [FromQuery] bool allUsers = false,
        [FromQuery] bool skipDateFilter = false)
    {
        try
        {
            IQueryable<WorkItem> query = _context.WorkItems;

            // Nếu allUsers = false, filter theo user hiện tại
            if (!allUsers)
            {
                // Lấy FirebaseUID từ JWT token để filter theo user hiện tại
                var firebaseUID = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst("sub")?.Value;
                
                if (string.IsNullOrEmpty(firebaseUID))
                {
                    _logger?.LogWarning("GetWorkItemsByDateRange - No FirebaseUID found in token");
                    return Unauthorized(new { error = "Invalid user token" });
                }

                // Lấy thông tin user từ FirebaseUID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
                
                if (user == null)
                {
                    // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                    var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                                    ?? User.FindFirst("email")?.Value;
                    
                    if (!string.IsNullOrEmpty(emailClaim))
                    {
                        user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == emailClaim);
                    }
                    
                    if (user == null)
                    {
                        _logger?.LogWarning("GetWorkItemsByDateRange - User not found. FirebaseUID: {FirebaseUID}", firebaseUID);
                        return NotFound(new { error = "User not found" });
                    }
                }

                // Lấy username để filter theo PersonName trong WorkItems
                // PersonName có thể là: UserId (string), FullName, hoặc UserName
                var userName = user.UserName;
                var fullName = user.FullName;
                var userIdString = user.UserId.ToString();

                query = query.Where(wi => wi.PersonName == userName || 
                                       wi.PersonName == fullName || 
                                       wi.PersonName == userIdString);
            }

            // Filter by date range (check StartDate, ExpectedFinish, ActualFinish, or Assignment DeliveryDate)
            // Hiển thị tất cả công việc có ít nhất một trong các ngày nằm trong khoảng 7 ngày qua
            // HOẶC có Assignment với DeliveryDate trong khoảng
            // Mục đích: xem tổng quan công việc, không phân biệt trạng thái
            // Nếu skipDateFilter = true, bỏ qua filter ngày để hiển thị tất cả
            if (!skipDateFilter && !string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start) &&
                !string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                var endDateInclusive = end.Date.AddDays(1);
                query = query.Where(wi =>
                    // StartDate nằm trong khoảng
                    (wi.StartDate.HasValue && wi.StartDate.Value >= start.Date && wi.StartDate.Value < endDateInclusive) ||
                    // ExpectedFinish nằm trong khoảng
                    (wi.ExpectedFinish.HasValue && wi.ExpectedFinish.Value >= start.Date && wi.ExpectedFinish.Value < endDateInclusive) ||
                    // ActualFinish nằm trong khoảng
                    (wi.ActualFinish.HasValue && wi.ActualFinish.Value >= start.Date && wi.ActualFinish.Value < endDateInclusive) ||
                    // Assignment DeliveryDate nằm trong khoảng
                    (wi.MachineAssignment != null && 
                     wi.MachineAssignment.DeliveryDate.HasValue && 
                     wi.MachineAssignment.DeliveryDate.Value >= start.Date && 
                     wi.MachineAssignment.DeliveryDate.Value < endDateInclusive)
                );
            }
            else if (!skipDateFilter)
            {
                // Nếu không có date range, vẫn filter theo từng ngày nếu có
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startOnly))
                {
                    query = query.Where(wi =>
                        (wi.StartDate.HasValue && wi.StartDate.Value >= startOnly.Date) ||
                        (wi.ExpectedFinish.HasValue && wi.ExpectedFinish.Value >= startOnly.Date) ||
                        (wi.ActualFinish.HasValue && wi.ActualFinish.Value >= startOnly.Date) ||
                        (wi.MachineAssignment != null && 
                         wi.MachineAssignment.DeliveryDate.HasValue && 
                         wi.MachineAssignment.DeliveryDate.Value >= startOnly.Date)
                    );
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endOnly))
                {
                    var endDateInclusive = endOnly.Date.AddDays(1);
                    query = query.Where(wi =>
                        (wi.StartDate.HasValue && wi.StartDate.Value < endDateInclusive) ||
                        (wi.ExpectedFinish.HasValue && wi.ExpectedFinish.Value < endDateInclusive) ||
                        (wi.ActualFinish.HasValue && wi.ActualFinish.Value < endDateInclusive) ||
                        (wi.MachineAssignment != null && 
                         wi.MachineAssignment.DeliveryDate.HasValue && 
                         wi.MachineAssignment.DeliveryDate.Value < endDateInclusive)
                    );
                }
            }

            // Filter by work types
            if (workTypes != null && workTypes.Length > 0)
            {
                query = query.Where(wi => wi.WorkType != null && workTypes.Contains(wi.WorkType));
            }

            var workItems = await query
                .Include(wi => wi.MachineAssignment!)
                    .ThenInclude(ma => ma.TechnicalSheet)
                .OrderByDescending(wi => wi.StartDate ?? wi.ExpectedFinish ?? wi.ActualFinish)
                .ToListAsync();

            // Lấy danh sách PersonName để query Users
            var personNames = workItems
                .Where(wi => !string.IsNullOrEmpty(wi.PersonName))
                .Select(wi => wi.PersonName!)
                .Distinct()
                .ToList();

            // Query Users để lấy FullName
            var users = await _context.Users
                .Where(u => personNames.Contains(u.UserName) || 
                           personNames.Contains(u.FullName ?? "") ||
                           personNames.Contains(u.UserId.ToString()))
                .ToListAsync();

            // Tạo dictionary để map PersonName -> FullName
            var personNameToFullName = new Dictionary<string, string>();
            foreach (var u in users)
            {
                if (!string.IsNullOrEmpty(u.UserName))
                    personNameToFullName[u.UserName] = u.FullName ?? u.UserName;
                if (!string.IsNullOrEmpty(u.FullName))
                    personNameToFullName[u.FullName] = u.FullName;
                personNameToFullName[u.UserId.ToString()] = u.FullName ?? u.UserName ?? "";
            }

            // Lấy danh sách TBKT_ID để query TechnicalSheet trực tiếp (fallback nếu navigation property không hoạt động)
            var tbktIds = workItems
                .Where(wi => wi.MachineAssignment != null && !string.IsNullOrEmpty(wi.MachineAssignment.TBKT_ID))
                .Select(wi => wi.MachineAssignment!.TBKT_ID)
                .Distinct()
                .ToList();

            // Query TechnicalSheet trực tiếp
            var technicalSheets = await _context.TechnicalSheets
                .Where(ts => tbktIds.Contains(ts.TBKT_ID))
                .ToListAsync();

            // Tạo dictionary để map TBKT_ID -> TechnicalSheet
            var tbktIdToTechnicalSheet = technicalSheets.ToDictionary(ts => ts.TBKT_ID);

            var workItemDtos = workItems.Select(wi => 
            {
                // Lấy TBKT_ID trực tiếp từ MachineAssignment.TBKT_ID
                var tbktId = !string.IsNullOrEmpty(wi.MachineAssignment?.TBKT_ID) 
                    ? wi.MachineAssignment.TBKT_ID 
                    : null;

                // Lấy Power_kVA từ TechnicalSheet nếu có
                var technicalSheet = wi.MachineAssignment?.TechnicalSheet;
                if (technicalSheet == null && wi.MachineAssignment != null && !string.IsNullOrEmpty(wi.MachineAssignment.TBKT_ID))
                {
                    tbktIdToTechnicalSheet.TryGetValue(wi.MachineAssignment.TBKT_ID, out technicalSheet);
                }
                var powerKVA = technicalSheet?.Power_kVA;

                return new WorkItemDto
                {
                    WorkItemID = wi.WorkItemID,
                    AssignmentID = wi.AssignmentID,
                    WorkType = wi.WorkType,
                    PersonName = wi.PersonName,
                    FullName = !string.IsNullOrEmpty(wi.PersonName) && personNameToFullName.TryGetValue(wi.PersonName, out var fullName) 
                        ? fullName 
                        : wi.PersonName,
                    StartDate = wi.StartDate,
                    ExpectedFinish = wi.ExpectedFinish,
                    ActualFinish = wi.ActualFinish,
                    PersonConfirmation = wi.PersonConfirmation,
                    Notes = wi.Notes,
                    File_ID = wi.File_ID,
                    MachineName = wi.MachineAssignment?.MachineName,
                    TBKT_ID = tbktId,
                    Power_kVA = technicalSheet?.Power_kVA,
                    DeliveryDate = wi.MachineAssignment?.DeliveryDate // Ngày hoàn thành của TBKT tổng
                };
            });

            return Ok(workItemDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWorkItemsByDateRange: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error getting work items by date range", message = ex.Message });
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
        // Kiểm tra lock status trước khi vào transaction (nếu PersonConfirmation = true)
        if (dto.PersonConfirmation == true)
        {
            var assignmentCheck = await _context.MachineAssignments
                .FirstOrDefaultAsync(a => a.AssignmentID == dto.AssignmentID);
            
            if (assignmentCheck == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            if (assignmentCheck.IsLocked)
            {
                _logger?.LogWarning("Cannot create work item with confirmation - assignment {AssignmentID} is locked", dto.AssignmentID);
                return BadRequest(new { error = "Cannot create work item", message = "Assignment đã bị khóa. Vui lòng liên hệ user kiểm soát để mở khóa." });
            }
        }

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
                    
                    // Khi user tạo work item với xác nhận (PersonConfirmation = true), khóa assignment
                    // Trạng thái sẽ được tự động tính lại bởi AssignmentStatusHelper
                    if (dto.PersonConfirmation == true)
                    {
                        // Tự động khóa assignment khi user thiết kế xác nhận hoàn thành
                        assignment.IsLocked = true;
                        _logger?.LogInformation("Locked MachineAssignment {AssignmentID} after creating work item with confirmation", assignment.AssignmentID);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Cập nhật trạng thái tổng của assignment dựa trên tất cả work items
                    await AssignmentStatusHelper.UpdateAssignmentStatusAsync(
                        _context, 
                        workItem.AssignmentID, 
                        _logger);
                    
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

        // Kiểm tra lock status trước khi vào transaction
        var workItemCheck = await _context.WorkItems
            .Include(wi => wi.MachineAssignment)
            .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
        
        if (workItemCheck == null)
        {
            return NotFound(new { error = "Work item not found", workItemID = id });
        }

        // Kiểm tra xem assignment có bị khóa không
        var assignmentCheck = workItemCheck.MachineAssignment;
        if (assignmentCheck == null)
        {
            assignmentCheck = await _context.MachineAssignments
                .FirstOrDefaultAsync(a => a.AssignmentID == workItemCheck.AssignmentID);
        }

        // Cho phép Manager/Administrator update ngay cả khi assignment bị locked
        // Cho phép user kiểm soát (review workitem) xác nhận ngay cả khi assignment bị locked
        // Cho phép user thiết kế (design workitem) chỉnh sửa và xác nhận khi chưa xác nhận, ngay cả khi assignment bị locked
        bool isManagerOrAdmin = RoleHelper.IsAdministratorOrManager(User);
        bool canBypassLock = isManagerOrAdmin;
        
        // Nếu không phải Manager/Admin, kiểm tra xem có phải user được gán cho workitem này không
        // Cho phép user chỉnh sửa workitem của họ khi chưa xác nhận, bất kể loại workitem
        if (!canBypassLock && assignmentCheck != null && assignmentCheck.IsLocked)
        {
            // Kiểm tra xem có phải review workitem (Core Review hoặc Casing Review) không
            bool isReviewWorkItem = workItemCheck.WorkType == "Core Review" || workItemCheck.WorkType == "Casing Review";
            // Kiểm tra xem có phải design workitem (Core Design hoặc Casing Design) không
            bool isDesignWorkItem = workItemCheck.WorkType == "Core Design" || workItemCheck.WorkType == "Casing Design";
            // Kiểm tra xem có phải material leveling workitem không
            bool isMaterialLeveling = workItemCheck.WorkType == "Material Leveling";
            
            // Cho phép bypass lock cho tất cả loại workitem nếu user được gán và chưa xác nhận
            if (isReviewWorkItem || isDesignWorkItem || isMaterialLeveling)
            {
                // Lấy FirebaseUID từ JWT token
                var firebaseUID = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (!string.IsNullOrEmpty(firebaseUID))
                {
                    // Lấy thông tin user từ database bằng FirebaseUID
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
                    
                    if (user != null)
                    {
                        // PersonName có thể là: UserId (string), FullName, hoặc UserName
                        var userIdString = user.UserId.ToString();
                        var fullName = user.FullName ?? "";
                        var userName = user.UserName ?? "";
                        
                        // Kiểm tra xem user có phải là người được gán cho workitem này không
                        bool isAssignedUser = workItemCheck.PersonName == userIdString || 
                                             workItemCheck.PersonName == fullName ||
                                             workItemCheck.PersonName == userName;
                        
                        if (isAssignedUser)
                        {
                            // Kiểm tra xem workitem đã được xác nhận chưa
                            bool isNotConfirmed = !workItemCheck.PersonConfirmation.GetValueOrDefault();
                            
                            if (isReviewWorkItem)
                            {
                                // User kiểm soát có thể xác nhận review workitem
                                canBypassLock = true;
                                _logger?.LogInformation("User kiểm soát {UserId} ({FullName}) bypassing lock to confirm review workitem {WorkItemID} - assignment {AssignmentID}", 
                                    user.UserId, user.FullName, id, assignmentCheck.AssignmentID);
                            }
                            else if (isDesignWorkItem && isNotConfirmed)
                            {
                                // User thiết kế có thể chỉnh sửa và xác nhận design workitem khi chưa xác nhận
                                canBypassLock = true;
                                _logger?.LogInformation("User thiết kế {UserId} ({FullName}) bypassing lock to edit/confirm design workitem {WorkItemID} (chưa xác nhận) - assignment {AssignmentID}", 
                                    user.UserId, user.FullName, id, assignmentCheck.AssignmentID);
                            }
                            else if (isMaterialLeveling && isNotConfirmed)
                            {
                                // User vật tư có thể chỉnh sửa và xác nhận material leveling workitem khi chưa xác nhận
                                canBypassLock = true;
                                _logger?.LogInformation("User vật tư {UserId} ({FullName}) bypassing lock to edit/confirm material leveling workitem {WorkItemID} (chưa xác nhận) - assignment {AssignmentID}", 
                                    user.UserId, user.FullName, id, assignmentCheck.AssignmentID);
                            }
                        }
                    }
                }
            }
        }
        
        if (assignmentCheck != null && assignmentCheck.IsLocked && !canBypassLock)
        {
            _logger?.LogWarning("Cannot update work item {WorkItemID} - assignment {AssignmentID} is locked and user does not have permission", id, assignmentCheck.AssignmentID);
            return BadRequest(new { error = "Cannot update work item", message = "Assignment đã bị khóa. Vui lòng liên hệ user kiểm soát để mở khóa." });
        }
        
        if (assignmentCheck != null && assignmentCheck.IsLocked && canBypassLock)
        {
            _logger?.LogInformation("Bypassing lock - updating work item {WorkItemID} - assignment {AssignmentID}", id, assignmentCheck.AssignmentID);
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
                    // Get work item from database with assignment
                    workItem = await _context.WorkItems
                        .Include(wi => wi.MachineAssignment)
                        .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
                    
                    if (workItem == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }
                    
                    _logger?.LogInformation("Found work item {WorkItemID} for update", id);

                    // Lấy assignment reference
                    var assignment = workItem.MachineAssignment;
                    if (assignment == null)
                    {
                        assignment = await _context.MachineAssignments
                            .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                    }

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
                        // DateOnlyJsonConverter already ensures the date is local, just extract date part
                        workItem.StartDate = new DateTime(dto.StartDate.Value.Year, dto.StartDate.Value.Month, dto.StartDate.Value.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    }

                    if (dto.ExpectedFinish.HasValue)
                    {
                        // DateOnlyJsonConverter already ensures the date is local, just extract date part
                        workItem.ExpectedFinish = new DateTime(dto.ExpectedFinish.Value.Year, dto.ExpectedFinish.Value.Month, dto.ExpectedFinish.Value.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    }

                    if (dto.ActualFinish.HasValue)
                    {
                        // DateOnlyJsonConverter already ensures the date is local, just extract date part
                        var dateOnly = new DateTime(dto.ActualFinish.Value.Year, dto.ActualFinish.Value.Month, dto.ActualFinish.Value.Day, 0, 0, 0, DateTimeKind.Unspecified);
                        workItem.ActualFinish = dateOnly;
                    }

                    if (dto.PersonConfirmation.HasValue)
                    {
                        workItem.PersonConfirmation = dto.PersonConfirmation.Value;
                        
                        // Khi user xác nhận (PersonConfirmation = true), khóa assignment
                        // Trạng thái sẽ được tự động tính lại bởi AssignmentStatusHelper
                        if (dto.PersonConfirmation.Value == true)
                        {
                            if (assignment == null)
                            {
                                assignment = await _context.MachineAssignments
                                    .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                            }
                            
                            if (assignment != null)
                            {
                                // Tự động khóa assignment khi user thiết kế xác nhận hoàn thành
                                assignment.IsLocked = true;
                                _logger?.LogInformation("Locked MachineAssignment {AssignmentID} after user confirmation", assignment.AssignmentID);
                            }
                        }
                        // Khi user kiểm soát từ chối (PersonConfirmation = false cho review workitem), reset design workitem và unlock assignment
                        else if (dto.PersonConfirmation.Value == false)
                        {
                            bool isReviewWorkItem = workItem.WorkType == "Core Review" || workItem.WorkType == "Casing Review";
                            
                            if (isReviewWorkItem)
                            {
                                if (assignment == null)
                                {
                                    assignment = await _context.MachineAssignments
                                        .FirstOrDefaultAsync(a => a.AssignmentID == workItem.AssignmentID);
                                }
                                
                                if (assignment != null)
                                {
                                    // Tìm design workitem tương ứng
                                    string designWorkType = workItem.WorkType == "Core Review" ? "Core Design" : "Casing Design";
                                    
                                    var designWorkItem = await _context.WorkItems
                                        .FirstOrDefaultAsync(wi => 
                                            wi.AssignmentID == workItem.AssignmentID && 
                                            wi.WorkType == designWorkType);
                                    
                                    if (designWorkItem != null)
                                    {
                                    // Reset design workitem về chưa hoàn thành
                                    designWorkItem.PersonConfirmation = false;
                                    designWorkItem.ActualFinish = null;
                                    _logger?.LogInformation("Reset design workitem {DesignWorkItemID} (WorkType: {WorkType}) to not completed after review workitem {ReviewWorkItemID} rejection", 
                                        designWorkItem.WorkItemID, designWorkItem.WorkType, workItem.WorkItemID);
                                    
                                    // Tạo notification cho user thiết kế về việc bị từ chối
                                    await CreateNotificationForDesignUserRejectionAsync(designWorkItem, assignment, workItem.Notes);
                                }
                                
                                // Unlock assignment để user thiết kế có thể chỉnh sửa lại
                                assignment.IsLocked = false;
                                _logger?.LogInformation("Unlocked MachineAssignment {AssignmentID} after review workitem {ReviewWorkItemID} rejection", 
                                    assignment.AssignmentID, workItem.WorkItemID);
                            }
                        }
                    }
                    }

                    if (dto.Notes != null)
                    {
                        workItem.Notes = dto.Notes;
                    }

                    // Chỉ update File_ID nếu được cung cấp trong DTO và có giá trị hợp lệ (không null, không empty)
                    // Nếu không có trong DTO hoặc là null/empty, giữ nguyên giá trị hiện tại (không overwrite)
                    if (!string.IsNullOrWhiteSpace(dto.File_ID))
                    {
                        workItem.File_ID = dto.File_ID;
                        _logger?.LogInformation("Updated File_ID for WorkItem {WorkItemID} to {FileId}", id, dto.File_ID);
                    }
                    // Nếu dto.File_ID là null, empty, hoặc whitespace, giữ nguyên File_ID hiện tại
                    // Entity Framework sẽ giữ nguyên giá trị hiện tại trong database

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

                    // Nếu workitem thiết kế được xác nhận, tạo notification cho user kiểm soát tương ứng
                    if (dto.PersonConfirmation.HasValue && dto.PersonConfirmation.Value == true)
                    {
                        bool isDesignWorkItem = workItem.WorkType == "Core Design" || workItem.WorkType == "Casing Design";
                        
                        if (isDesignWorkItem && assignment != null)
                        {
                            // Tìm workitem kiểm soát tương ứng
                            string reviewWorkType = workItem.WorkType == "Core Design" ? "Core Review" : "Casing Review";
                            
                            var reviewWorkItem = await _context.WorkItems
                                .FirstOrDefaultAsync(wi => 
                                    wi.AssignmentID == workItem.AssignmentID && 
                                    wi.WorkType == reviewWorkType);
                            
                            if (reviewWorkItem != null && !string.IsNullOrWhiteSpace(reviewWorkItem.PersonName))
                            {
                                // Tạo notification cho user kiểm soát
                                await CreateNotificationForReviewUserAsync(reviewWorkItem, assignment);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    
                    // Cập nhật trạng thái tổng của assignment dựa trên tất cả work items
                    await AssignmentStatusHelper.UpdateAssignmentStatusAsync(
                        _context, 
                        workItem.AssignmentID, 
                        _logger);
                    
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

                    int assignmentId = workItem.AssignmentID;
                    _context.WorkItems.Remove(workItem);
                    await _context.SaveChangesAsync();
                    
                    // Cập nhật trạng thái tổng của assignment sau khi xóa work item
                    await AssignmentStatusHelper.UpdateAssignmentStatusAsync(
                        _context, 
                        assignmentId, 
                        _logger);
                    
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

    // Tạo notification cho user kiểm soát khi workitem thiết kế được xác nhận
    private async Task CreateNotificationForReviewUserAsync(WorkItem reviewWorkItem, MachineAssignment assignment)
    {
        if (string.IsNullOrWhiteSpace(reviewWorkItem.PersonName))
        {
            _logger?.LogWarning("Review WorkItem {WorkItemID} has no PersonName, skipping notification", reviewWorkItem.WorkItemID);
            return;
        }

        // Find user kiểm soát by PersonName (could be UserName, FullName, or UserId)
        var reviewUser = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.UserName == reviewWorkItem.PersonName || 
                u.FullName == reviewWorkItem.PersonName ||
                u.UserId.ToString() == reviewWorkItem.PersonName);

        if (reviewUser == null || string.IsNullOrEmpty(reviewUser.FirebaseUID))
        {
            _logger?.LogWarning("Review user not found for PersonName: {PersonName}, skipping notification", reviewWorkItem.PersonName);
            return;
        }

        // Kiểm tra xem notification đã tồn tại chưa (tránh duplicate)
        var existingNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => 
                n.RelatedEntityType == "WorkItem" && 
                n.RelatedEntityId == reviewWorkItem.WorkItemID &&
                n.UserId == reviewUser.FirebaseUID &&
                !n.IsRead);

        if (existingNotification != null)
        {
            _logger?.LogInformation("Notification already exists for review workitem {WorkItemID}, skipping", reviewWorkItem.WorkItemID);
            return;
        }

        // Get TBKT_ID from assignment
        var assignmentWithTBKT = await _context.MachineAssignments
            .Include(a => a.TechnicalSheet)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);
        
        var tbktId = assignmentWithTBKT?.TechnicalSheet?.TBKT_ID ?? "N/A";
        
        // Create notification cho user kiểm soát
        var notification = new Notification
        {
            UserId = reviewUser.FirebaseUID,
            Title = "Công việc thiết kế đã được xác nhận",
            Message = $"{assignment.MachineName} - {tbktId}",
            Type = "info",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedEntityType = "WorkItem",
            RelatedEntityId = reviewWorkItem.WorkItemID
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Created notification for review user {FirebaseUID} about work item {WorkItemID}", reviewUser.FirebaseUID, reviewWorkItem.WorkItemID);
        
        // Gửi SignalR notification
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"user_{reviewUser.FirebaseUID}").SendAsync("NewNotification", new 
            { 
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                createdAt = notification.CreatedAt
            });
            await _hubContext.Clients.Group($"user_{reviewUser.FirebaseUID}").SendAsync("UnreadCountChanged");
        }
    }

    // Tạo notification cho user thiết kế khi workitem của họ bị từ chối
    private async Task CreateNotificationForDesignUserRejectionAsync(WorkItem designWorkItem, MachineAssignment assignment, string? rejectionNotes)
    {
        if (string.IsNullOrWhiteSpace(designWorkItem.PersonName))
        {
            _logger?.LogWarning("Design WorkItem {WorkItemID} has no PersonName, skipping rejection notification", designWorkItem.WorkItemID);
            return;
        }

        // Find user thiết kế by PersonName (could be UserName, FullName, or UserId)
        var designUser = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.UserName == designWorkItem.PersonName || 
                u.FullName == designWorkItem.PersonName ||
                u.UserId.ToString() == designWorkItem.PersonName);

        if (designUser == null || string.IsNullOrEmpty(designUser.FirebaseUID))
        {
            _logger?.LogWarning("Design user not found for PersonName: {PersonName}, skipping rejection notification", designWorkItem.PersonName);
            return;
        }

        // Get TBKT_ID from assignment
        var assignmentWithTBKT = await _context.MachineAssignments
            .Include(a => a.TechnicalSheet)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);
        
        var tbktId = assignmentWithTBKT?.TechnicalSheet?.TBKT_ID ?? "N/A";
        
        // Tạo message với lý do từ chối nếu có
        var message = tbktId;
        if (!string.IsNullOrWhiteSpace(rejectionNotes))
        {
            message = $"{tbktId} - {rejectionNotes}";
        }
        
        // Create notification cho user thiết kế
        var notification = new Notification
        {
            UserId = designUser.FirebaseUID,
            Title = "Công việc của bạn đã bị từ chối",
            Message = message,
            Type = "warning",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedEntityType = "WorkItem",
            RelatedEntityId = designWorkItem.WorkItemID
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Created rejection notification for design user {FirebaseUID} about work item {WorkItemID}", designUser.FirebaseUID, designWorkItem.WorkItemID);
        
        // Gửi SignalR notification
        if (_hubContext != null)
        {
            await _hubContext.Clients.Group($"user_{designUser.FirebaseUID}").SendAsync("NewNotification", new 
            { 
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                createdAt = notification.CreatedAt
            });
            await _hubContext.Clients.Group($"user_{designUser.FirebaseUID}").SendAsync("UnreadCountChanged");
        }
    }
}

