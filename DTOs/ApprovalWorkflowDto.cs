namespace quanlyfilesBE.DTOs;

public class ApprovalWorkflowDto
{
    public int WorkflowID { get; set; }
    public string? RequestTitle { get; set; }
    public string? RequestDescription { get; set; }
    public string? RequestType { get; set; }
    public string? RequestReferenceID { get; set; }
    
    public string? RequesterFirebaseUID { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterEmail { get; set; }
    
    public DateTime? RequestSentDate { get; set; }
    public string? RequestStatus { get; set; }
    
    public string? ControllerFirebaseUID { get; set; }
    public string? ControllerName { get; set; }
    public string? ControllerEmail { get; set; }
    public DateTime? ControlReviewDate { get; set; }
    public string? ControlStatus { get; set; }
    public string? ControlNotes { get; set; }
    
    public string? ApproverFirebaseUID { get; set; }
    public string? ApproverName { get; set; }
    public string? ApproverEmail { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? ApprovalNotes { get; set; }
    
    public string? OverallStatus { get; set; }
    public DateTime? CompletedDate { get; set; }
    
    public string? PowerAutomateFlowRunID { get; set; }
    public string? PowerAutomateFlowURL { get; set; }
    public DateTime? LastNotificationSent { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateApprovalWorkflowDto
{
    public string? RequestTitle { get; set; }
    public string? RequestDescription { get; set; }
    public string? RequestType { get; set; }
    public string? RequestReferenceID { get; set; }
    
    public string? ControllerEmail { get; set; }
    public string? ApproverEmail { get; set; }
}

public class UpdateApprovalWorkflowDto
{
    public string? RequestTitle { get; set; }
    public string? RequestDescription { get; set; }
    public string? ControlNotes { get; set; }
    public string? ApprovalNotes { get; set; }
}

public class SubmitApprovalActionDto
{
    public int WorkflowID { get; set; }
    public string Action { get; set; } = string.Empty; // "approve", "reject"
    public string? Notes { get; set; }
    public string? UserRole { get; set; } // "controller", "approver"
}

