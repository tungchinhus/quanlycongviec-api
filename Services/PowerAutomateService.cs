using System.Net.Http.Json;
using System.Text.Json;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Services;

public interface IPowerAutomateService
{
    Task<bool> TriggerApprovalFlowAsync(ApprovalWorkflowDto workflow, string action);
    Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string body, ApprovalWorkflowDto workflow);
}

public class PowerAutomateService : IPowerAutomateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PowerAutomateService>? _logger;
    private readonly IConfiguration _configuration;

    public PowerAutomateService(
        IHttpClientFactory httpClientFactory,
        ILogger<PowerAutomateService>? logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> TriggerApprovalFlowAsync(ApprovalWorkflowDto workflow, string action)
    {
        try
        {
            // Lấy Power Automate Flow URL từ configuration
            var flowUrl = _configuration["PowerAutomate:FlowURL"];
            if (string.IsNullOrEmpty(flowUrl))
            {
                _logger?.LogWarning("PowerAutomate:FlowURL is not configured. Skipping flow trigger.");
                return false;
            }

            // Tạo payload động với đầy đủ thông tin
            var payload = new
            {
                // Thông tin workflow
                workflowId = workflow.WorkflowID,
                requestTitle = workflow.RequestTitle ?? string.Empty,
                requestDescription = workflow.RequestDescription ?? string.Empty,
                requestType = workflow.RequestType ?? string.Empty,
                requestReferenceID = workflow.RequestReferenceID ?? string.Empty,
                
                // Thông tin người gửi
                requesterName = workflow.RequesterName ?? string.Empty,
                requesterEmail = workflow.RequesterEmail ?? string.Empty,
                requesterFirebaseUID = workflow.RequesterFirebaseUID ?? string.Empty,
                requestSentDate = workflow.RequestSentDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                requestStatus = workflow.RequestStatus ?? string.Empty,
                
                // Thông tin kiểm soát
                controllerName = workflow.ControllerName ?? string.Empty,
                controllerEmail = workflow.ControllerEmail ?? string.Empty,
                controllerFirebaseUID = workflow.ControllerFirebaseUID ?? string.Empty,
                controlStatus = workflow.ControlStatus ?? "Pending",
                controlReviewDate = workflow.ControlReviewDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                controlNotes = workflow.ControlNotes ?? string.Empty,
                
                // Thông tin xét duyệt
                approverName = workflow.ApproverName ?? string.Empty,
                approverEmail = workflow.ApproverEmail ?? string.Empty,
                approverFirebaseUID = workflow.ApproverFirebaseUID ?? string.Empty,
                approvalStatus = workflow.ApprovalStatus ?? "Pending",
                approvalDate = workflow.ApprovalDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                approvalNotes = workflow.ApprovalNotes ?? string.Empty,
                
                // Trạng thái tổng thể
                overallStatus = workflow.OverallStatus ?? string.Empty,
                completedDate = workflow.CompletedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                
                // Action và metadata
                action = action, // "send_request", "control_approved", "control_rejected", "approval_completed", "approval_rejected"
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                
                // Thông tin bổ sung cho Power Automate
                workflowUrl = $"{_configuration["AppSettings:BaseUrl"] ?? "http://localhost:4200"}/approval-workflow",
                viewWorkflowUrl = $"{_configuration["AppSettings:BaseUrl"] ?? "http://localhost:4200"}/approval-workflow?workflowId={workflow.WorkflowID}"
            };

            var response = await _httpClient.PostAsJsonAsync(flowUrl, payload);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation("Power Automate flow triggered successfully. Response: {Response}", responseContent);
                
                // Parse response để lấy flow run ID nếu có
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (jsonResponse.TryGetProperty("flowRunId", out var runIdElement))
                    {
                        workflow.PowerAutomateFlowRunID = runIdElement.GetString();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not parse Power Automate response for flow run ID");
                }
                
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Power Automate flow trigger failed. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error triggering Power Automate flow");
            return false;
        }
    }

    public async Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string body, ApprovalWorkflowDto workflow)
    {
        try
        {
            // Lấy Power Automate Email Notification Flow URL từ configuration
            var emailFlowUrl = _configuration["PowerAutomate:EmailNotificationFlowURL"];
            if (string.IsNullOrEmpty(emailFlowUrl))
            {
                _logger?.LogWarning("PowerAutomate:EmailNotificationFlowURL is not configured. Skipping email notification.");
                return false;
            }

            // Tạo payload động với đầy đủ thông tin cho email
            var payload = new
            {
                // Thông tin email
                toEmail = toEmail,
                subject = subject,
                body = body,
                
                // Thông tin workflow đầy đủ
                workflowId = workflow.WorkflowID,
                requestTitle = workflow.RequestTitle ?? string.Empty,
                requestDescription = workflow.RequestDescription ?? string.Empty,
                requestType = workflow.RequestType ?? string.Empty,
                requestReferenceID = workflow.RequestReferenceID ?? string.Empty,
                
                // Thông tin người gửi
                requesterName = workflow.RequesterName ?? string.Empty,
                requesterEmail = workflow.RequesterEmail ?? string.Empty,
                
                // Thông tin kiểm soát
                controllerName = workflow.ControllerName ?? string.Empty,
                controllerEmail = workflow.ControllerEmail ?? string.Empty,
                controlStatus = workflow.ControlStatus ?? "Pending",
                controlNotes = workflow.ControlNotes ?? string.Empty,
                
                // Thông tin xét duyệt
                approverName = workflow.ApproverName ?? string.Empty,
                approverEmail = workflow.ApproverEmail ?? string.Empty,
                approvalStatus = workflow.ApprovalStatus ?? "Pending",
                approvalNotes = workflow.ApprovalNotes ?? string.Empty,
                
                // Trạng thái
                overallStatus = workflow.OverallStatus ?? string.Empty,
                
                // URLs để truy cập workflow
                workflowUrl = $"{_configuration["AppSettings:BaseUrl"] ?? "http://localhost:4200"}/approval-workflow",
                viewWorkflowUrl = $"{_configuration["AppSettings:BaseUrl"] ?? "http://localhost:4200"}/approval-workflow?workflowId={workflow.WorkflowID}",
                
                // Timestamp
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var response = await _httpClient.PostAsJsonAsync(emailFlowUrl, payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger?.LogInformation("Email notification sent successfully to {Email}", toEmail);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Email notification failed. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending email notification");
            return false;
        }
    }
}

