namespace quanlyfilesBE.Models;

public class TechnicalSheetApproval
{
    public int ApprovalID { get; set; }
    public string TBKT_ID { get; set; } = string.Empty;
    public string ApprovalLevel { get; set; } = string.Empty; // 'ManagerL1' hoặc 'Manager'
    public string ApprovalStatus { get; set; } = string.Empty; // 'Pending', 'Approved', 'Rejected'
    public string ApproverFirebaseUID { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public DateTime ApprovalDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public TechnicalSheet? TechnicalSheet { get; set; }
}
