namespace quanlyfilesBE.Models;

public class MachineAssignment
{
    public int AssignmentID { get; set; }
    public string TBKT_ID { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? StandardRequirement { get; set; }
    public string? AdditionalRequest { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Designer { get; set; }
    public string? TeamLeader { get; set; }
    public string? FilePath { get; set; }
    public int Status { get; set; } = 1; // 1: new, 2: đang xử lý, 3: hoàn thành

    // Navigation properties
    public TechnicalSheet? TechnicalSheet { get; set; }
    public ICollection<AssignmentApproval> AssignmentApprovals { get; set; } = new List<AssignmentApproval>();
    public ICollection<WorkChange> WorkChanges { get; set; } = new List<WorkChange>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}

