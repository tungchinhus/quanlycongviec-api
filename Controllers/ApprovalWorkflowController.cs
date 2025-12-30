using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/approval-workflow")]
[Authorize]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalWorkflowController>? _logger;
    private readonly IPowerAutomateService _powerAutomateService;
    private readonly IFileLoggerService _fileLogger;

    public ApprovalWorkflowController(
        ApplicationDbContext context,
        ILogger<ApprovalWorkflowController>? logger = null,
        IPowerAutomateService? powerAutomateService = null,
        IFileLoggerService? fileLogger = null)
    {
        _context = context;
        _logger = logger;
        _powerAutomateService = powerAutomateService ?? throw new ArgumentNullException(nameof(powerAutomateService));
        _fileLogger = fileLogger ?? new FileLoggerService();
    }

    // GET: api/approval-workflow
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApprovalWorkflowDto>>> GetAll([FromQuery] string? status = null)
    {
        try
        {
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;

            IQueryable<ApprovalWorkflow> query = _context.ApprovalWorkflows;

            // Filter theo status nếu có
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(w => w.OverallStatus == status);
            }

            // User thường chỉ xem được workflow của mình hoặc workflow mà họ là controller/approver
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            if (!isAdminOrManager && !string.IsNullOrEmpty(currentUserFirebaseUID))
            {
                query = query.Where(w => 
                    w.RequesterFirebaseUID == currentUserFirebaseUID ||
                    w.ControllerFirebaseUID == currentUserFirebaseUID ||
                    w.ApproverFirebaseUID == currentUserFirebaseUID);
            }

            var workflows = await query
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var dtos = workflows.Select(w => MapToDto(w)).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting approval workflows");
            await _fileLogger.LogErrorAsync("ApprovalWorkflowController.GetAll: Error", ex);
            return StatusCode(500, new { error = "Error retrieving approval workflows", message = ex.Message });
        }
    }

    // GET: api/approval-workflow/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> GetById(int id)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(id);
            if (workflow == null)
            {
                return NotFound(new { message = $"Approval workflow with ID {id} not found" });
            }

            // Kiểm tra quyền truy cập
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);

            if (!isAdminOrManager && !string.IsNullOrEmpty(currentUserFirebaseUID))
            {
                if (workflow.RequesterFirebaseUID != currentUserFirebaseUID &&
                    workflow.ControllerFirebaseUID != currentUserFirebaseUID &&
                    workflow.ApproverFirebaseUID != currentUserFirebaseUID)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền xem workflow này" });
                }
            }

            return Ok(MapToDto(workflow));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting approval workflow by ID: {Id}", id);
            await _fileLogger.LogErrorAsync($"ApprovalWorkflowController.GetById: Error for ID {id}", ex);
            return StatusCode(500, new { error = "Error retrieving approval workflow", message = ex.Message });
        }
    }

    // POST: api/approval-workflow
    [HttpPost]
    public async Task<ActionResult<ApprovalWorkflowDto>> Create([FromBody] CreateApprovalWorkflowDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy thông tin user hiện tại
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value 
                ?? User?.FindFirst("email")?.Value;
            var currentUserName = User?.Identity?.Name ?? "Unknown";

            // Tìm user trong database để lấy thông tin đầy đủ
            User? currentUser = null;
            if (!string.IsNullOrEmpty(currentUserFirebaseUID))
            {
                currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUID == currentUserFirebaseUID);
            }

            // Tìm controller và approver từ email
            User? controllerUser = null;
            User? approverUser = null;

            if (!string.IsNullOrEmpty(dto.ControllerEmail))
            {
                controllerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.ControllerEmail);
            }

            if (!string.IsNullOrEmpty(dto.ApproverEmail))
            {
                approverUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.ApproverEmail);
            }

            var workflow = new ApprovalWorkflow
            {
                RequestTitle = dto.RequestTitle,
                RequestDescription = dto.RequestDescription,
                RequestType = dto.RequestType,
                RequestReferenceID = dto.RequestReferenceID,
                
                RequesterFirebaseUID = currentUserFirebaseUID,
                RequesterName = currentUser?.FullName ?? currentUserName,
                RequesterEmail = currentUser?.Email ?? currentUserEmail,
                
                ControllerFirebaseUID = controllerUser?.FirebaseUID,
                ControllerName = controllerUser?.FullName,
                ControllerEmail = controllerUser?.Email ?? dto.ControllerEmail,
                
                ApproverFirebaseUID = approverUser?.FirebaseUID,
                ApproverName = approverUser?.FullName,
                ApproverEmail = approverUser?.Email ?? dto.ApproverEmail,
                
                RequestStatus = "Sent",
                ControlStatus = "Pending",
                ApprovalStatus = "Pending",
                OverallStatus = "PendingControl",
                RequestSentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserFirebaseUID
            };

            _context.ApprovalWorkflows.Add(workflow);
            await _context.SaveChangesAsync();

            var workflowDto = MapToDto(workflow);

            // Gửi email thông báo cho kiểm soát thông qua Power Automate (không làm fail request nếu có lỗi)
            if (!string.IsNullOrEmpty(workflow.ControllerEmail))
            {
                try
                {
                    // Truyền thông tin động để Power Automate format email
                    var emailSubject = $"Yêu cầu ký duyệt: {workflow.RequestTitle}";
                    // Email body đơn giản, Power Automate sẽ format đẹp hơn từ các field riêng lẻ
                    var emailBody = $"Bạn có một yêu cầu ký duyệt mới cần được kiểm soát. Vui lòng đăng nhập hệ thống để xem chi tiết.";

                    await _powerAutomateService.SendNotificationEmailAsync(
                        workflow.ControllerEmail,
                        emailSubject,
                        emailBody,
                        workflowDto);

                    // Trigger Power Automate flow
                    await _powerAutomateService.TriggerApprovalFlowAsync(workflowDto, "send_request");

                    workflow.LastNotificationSent = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch (Exception powerAutomateEx)
                {
                    // Log lỗi nhưng không làm fail request
                    _logger?.LogWarning(powerAutomateEx, "Power Automate notification failed but workflow was created successfully. WorkflowID: {WorkflowID}", workflow.WorkflowID);
                    await _fileLogger.LogWarningAsync($"ApprovalWorkflowController.Create: Power Automate notification failed for workflow {workflow.WorkflowID}. Exception: {powerAutomateEx.Message}");
                }
            }

            _logger?.LogInformation("Approval workflow created: {WorkflowID}", workflow.WorkflowID);
            await _fileLogger.LogInfoAsync($"ApprovalWorkflowController.Create: Created workflow {workflow.WorkflowID}");

            return CreatedAtAction(nameof(GetById), new { id = workflow.WorkflowID }, workflowDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating approval workflow");
            await _fileLogger.LogErrorAsync("ApprovalWorkflowController.Create: Error", ex);
            return StatusCode(500, new { error = "Error creating approval workflow", message = ex.Message });
        }
    }

    // POST: api/approval-workflow/{id}/submit-action
    [HttpPost("{id}/submit-action")]
    public async Task<ActionResult<ApprovalWorkflowDto>> SubmitAction(int id, [FromBody] SubmitApprovalActionDto dto)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(id);
            if (workflow == null)
            {
                return NotFound(new { message = $"Approval workflow with ID {id} not found" });
            }

            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value 
                ?? User?.FindFirst("email")?.Value;
            var currentUserName = User?.Identity?.Name ?? "Unknown";

            // Kiểm tra quyền
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            bool hasPermission = false;

            if (dto.UserRole == "controller")
            {
                hasPermission = isAdminOrManager || workflow.ControllerFirebaseUID == currentUserFirebaseUID;
                if (!hasPermission)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động kiểm soát" });
                }
            }
            else if (dto.UserRole == "approver")
            {
                hasPermission = isAdminOrManager || workflow.ApproverFirebaseUID == currentUserFirebaseUID;
                if (!hasPermission)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động xét duyệt" });
                }
            }

            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.UpdatedBy = currentUserFirebaseUID;

            if (dto.UserRole == "controller")
            {
                // Xử lý hành động kiểm soát
                workflow.ControlReviewDate = DateTime.UtcNow;
                workflow.ControlNotes = dto.Notes;

                if (dto.Action.ToLower() == "approve")
                {
                    workflow.ControlStatus = "Approved";
                    workflow.OverallStatus = "PendingApproval";

                    // Gửi email thông báo cho người xét duyệt
                    if (!string.IsNullOrEmpty(workflow.ApproverEmail))
                    {
                        var emailSubject = $"Yêu cầu ký duyệt đã được kiểm soát: {workflow.RequestTitle}";
                        // Email body đơn giản, Power Automate sẽ format từ các field động
                        var emailBody = $"Yêu cầu ký duyệt đã được kiểm soát và chuyển đến bạn để xét duyệt. Vui lòng đăng nhập hệ thống để xem chi tiết.";

                        var workflowDto = MapToDto(workflow);
                        await _powerAutomateService.SendNotificationEmailAsync(
                            workflow.ApproverEmail,
                            emailSubject,
                            emailBody,
                            workflowDto);

                        await _powerAutomateService.TriggerApprovalFlowAsync(workflowDto, "control_approved");
                        workflow.LastNotificationSent = DateTime.UtcNow;
                    }
                }
                else if (dto.Action.ToLower() == "reject")
                {
                    workflow.ControlStatus = "Rejected";
                    workflow.OverallStatus = "Rejected";
                    workflow.CompletedDate = DateTime.UtcNow;

                    // Gửi email thông báo từ chối cho người gửi
                    if (!string.IsNullOrEmpty(workflow.RequesterEmail))
                    {
                        var emailSubject = $"Yêu cầu ký duyệt đã bị từ chối: {workflow.RequestTitle}";
                        // Email body đơn giản, Power Automate sẽ format từ các field động
                        var emailBody = $"Yêu cầu ký duyệt của bạn đã bị từ chối ở cấp kiểm soát. Vui lòng đăng nhập hệ thống để xem chi tiết.";

                        var workflowDto = MapToDto(workflow);
                        await _powerAutomateService.SendNotificationEmailAsync(
                            workflow.RequesterEmail,
                            emailSubject,
                            emailBody,
                            workflowDto);
                        workflow.LastNotificationSent = DateTime.UtcNow;
                    }
                }
            }
            else if (dto.UserRole == "approver")
            {
                // Xử lý hành động xét duyệt
                workflow.ApprovalDate = DateTime.UtcNow;
                workflow.ApprovalNotes = dto.Notes;

                if (dto.Action.ToLower() == "approve")
                {
                    workflow.ApprovalStatus = "Approved";
                    workflow.OverallStatus = "Completed";
                    workflow.CompletedDate = DateTime.UtcNow;

                    // Gửi email thông báo hoàn tất cho người gửi
                    if (!string.IsNullOrEmpty(workflow.RequesterEmail))
                    {
                        var emailSubject = $"Yêu cầu ký duyệt đã được phê duyệt: {workflow.RequestTitle}";
                        // Email body đơn giản, Power Automate sẽ format từ các field động
                        var emailBody = $"Yêu cầu ký duyệt của bạn đã được phê duyệt hoàn tất. Vui lòng đăng nhập hệ thống để xem chi tiết.";

                        var workflowDto = MapToDto(workflow);
                        await _powerAutomateService.SendNotificationEmailAsync(
                            workflow.RequesterEmail,
                            emailSubject,
                            emailBody,
                            workflowDto);

                        await _powerAutomateService.TriggerApprovalFlowAsync(workflowDto, "approval_completed");
                        workflow.LastNotificationSent = DateTime.UtcNow;
                    }
                }
                else if (dto.Action.ToLower() == "reject")
                {
                    workflow.ApprovalStatus = "Rejected";
                    workflow.OverallStatus = "Rejected";
                    workflow.CompletedDate = DateTime.UtcNow;

                    // Gửi email thông báo từ chối cho người gửi
                    if (!string.IsNullOrEmpty(workflow.RequesterEmail))
                    {
                        var emailSubject = $"Yêu cầu ký duyệt đã bị từ chối: {workflow.RequestTitle}";
                        // Email body đơn giản, Power Automate sẽ format từ các field động
                        var emailBody = $"Yêu cầu ký duyệt của bạn đã bị từ chối ở cấp xét duyệt. Vui lòng đăng nhập hệ thống để xem chi tiết.";

                        var workflowDto = MapToDto(workflow);
                        await _powerAutomateService.SendNotificationEmailAsync(
                            workflow.RequesterEmail,
                            emailSubject,
                            emailBody,
                            workflowDto);
                        workflow.LastNotificationSent = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var resultDto = MapToDto(workflow);
            _logger?.LogInformation("Approval workflow action submitted: {WorkflowID}, Action: {Action}, Role: {Role}", 
                workflow.WorkflowID, dto.Action, dto.UserRole);
            await _fileLogger.LogInfoAsync($"ApprovalWorkflowController.SubmitAction: Action {dto.Action} submitted for workflow {workflow.WorkflowID}");

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error submitting approval action for workflow {Id}", id);
            await _fileLogger.LogErrorAsync($"ApprovalWorkflowController.SubmitAction: Error for workflow {id}", ex);
            return StatusCode(500, new { error = "Error submitting approval action", message = ex.Message });
        }
    }

    // PUT: api/approval-workflow/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> Update(int id, [FromBody] UpdateApprovalWorkflowDto dto)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(id);
            if (workflow == null)
            {
                return NotFound(new { message = $"Approval workflow with ID {id} not found" });
            }

            // Chỉ cho phép cập nhật khi workflow chưa hoàn tất
            if (workflow.OverallStatus == "Completed" || workflow.OverallStatus == "Rejected")
            {
                return BadRequest(new { message = "Không thể cập nhật workflow đã hoàn tất hoặc bị từ chối" });
            }

            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;

            // Chỉ người gửi hoặc admin mới có thể cập nhật
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            if (!isAdminOrManager && workflow.RequesterFirebaseUID != currentUserFirebaseUID)
            {
                return StatusCode(403, new { message = "Bạn không có quyền cập nhật workflow này" });
            }

            if (!string.IsNullOrEmpty(dto.RequestTitle))
                workflow.RequestTitle = dto.RequestTitle;
            if (!string.IsNullOrEmpty(dto.RequestDescription))
                workflow.RequestDescription = dto.RequestDescription;

            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.UpdatedBy = currentUserFirebaseUID;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(workflow));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating approval workflow {Id}", id);
            await _fileLogger.LogErrorAsync($"ApprovalWorkflowController.Update: Error for workflow {id}", ex);
            return StatusCode(500, new { error = "Error updating approval workflow", message = ex.Message });
        }
    }

    // DELETE: api/approval-workflow/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(id);
            if (workflow == null)
            {
                return NotFound(new { message = $"Approval workflow with ID {id} not found" });
            }

            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;

            // Chỉ người gửi hoặc admin mới có thể xóa
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            if (!isAdminOrManager && workflow.RequesterFirebaseUID != currentUserFirebaseUID)
            {
                return StatusCode(403, new { message = "Bạn không có quyền xóa workflow này" });
            }

            // Chỉ cho phép xóa khi workflow chưa được xử lý
            if (workflow.OverallStatus != "Draft" && workflow.OverallStatus != "PendingControl")
            {
                return BadRequest(new { message = "Không thể xóa workflow đã được xử lý" });
            }

            _context.ApprovalWorkflows.Remove(workflow);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting approval workflow {Id}", id);
            await _fileLogger.LogErrorAsync($"ApprovalWorkflowController.Delete: Error for workflow {id}", ex);
            return StatusCode(500, new { error = "Error deleting approval workflow", message = ex.Message });
        }
    }

    private ApprovalWorkflowDto MapToDto(ApprovalWorkflow workflow)
    {
        return new ApprovalWorkflowDto
        {
            WorkflowID = workflow.WorkflowID,
            RequestTitle = workflow.RequestTitle,
            RequestDescription = workflow.RequestDescription,
            RequestType = workflow.RequestType,
            RequestReferenceID = workflow.RequestReferenceID,
            RequesterFirebaseUID = workflow.RequesterFirebaseUID,
            RequesterName = workflow.RequesterName,
            RequesterEmail = workflow.RequesterEmail,
            RequestSentDate = workflow.RequestSentDate,
            RequestStatus = workflow.RequestStatus,
            ControllerFirebaseUID = workflow.ControllerFirebaseUID,
            ControllerName = workflow.ControllerName,
            ControllerEmail = workflow.ControllerEmail,
            ControlReviewDate = workflow.ControlReviewDate,
            ControlStatus = workflow.ControlStatus,
            ControlNotes = workflow.ControlNotes,
            ApproverFirebaseUID = workflow.ApproverFirebaseUID,
            ApproverName = workflow.ApproverName,
            ApproverEmail = workflow.ApproverEmail,
            ApprovalDate = workflow.ApprovalDate,
            ApprovalStatus = workflow.ApprovalStatus,
            ApprovalNotes = workflow.ApprovalNotes,
            OverallStatus = workflow.OverallStatus,
            CompletedDate = workflow.CompletedDate,
            PowerAutomateFlowRunID = workflow.PowerAutomateFlowRunID,
            PowerAutomateFlowURL = workflow.PowerAutomateFlowURL,
            LastNotificationSent = workflow.LastNotificationSent,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CreatedBy = workflow.CreatedBy,
            UpdatedBy = workflow.UpdatedBy
        };
    }
}

