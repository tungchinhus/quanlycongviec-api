namespace quanlyfilesBE.Models;

public class ApprovalWorkflow
{
    public int WorkflowID { get; set; }
    
    // Thông tin yêu cầu
    public string? RequestTitle { get; set; }
    public string? RequestDescription { get; set; }
    public string? RequestType { get; set; } // Loại yêu cầu (TechnicalSheet, Assignment, etc.)
    public string? RequestReferenceID { get; set; } // ID tham chiếu (TBKT_ID, AssignmentID, OneDrive URL, etc.)
    
    // Người gửi yêu cầu
    public string? RequesterFirebaseUID { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterEmail { get; set; }
    
    // Cấp 1: Gửi yêu cầu
    public DateTime? RequestSentDate { get; set; }
    public string? RequestStatus { get; set; } // Pending, Sent, Cancelled
    
    // Cấp 2: Kiểm soát
    public string? ControllerFirebaseUID { get; set; }
    public string? ControllerName { get; set; }
    public string? ControllerEmail { get; set; }
    public DateTime? ControlReviewDate { get; set; }
    public string? ControlStatus { get; set; } // Pending, Approved, Rejected
    public string? ControlNotes { get; set; }
    
    // Cấp 3: Xét duyệt
    public string? ApproverFirebaseUID { get; set; }
    public string? ApproverName { get; set; }
    public string? ApproverEmail { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalStatus { get; set; } // Pending, Approved, Rejected
    public string? ApprovalNotes { get; set; }
    
    // Trạng thái tổng thể
    public string? OverallStatus { get; set; } // Draft, PendingControl, PendingApproval, Approved, Rejected, Completed
    public DateTime? CompletedDate { get; set; }
    
    // Power Automate integration
    public string? PowerAutomateFlowRunID { get; set; }
    public string? PowerAutomateFlowURL { get; set; }
    public DateTime? LastNotificationSent { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

