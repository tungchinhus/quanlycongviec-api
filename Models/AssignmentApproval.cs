namespace quanlyfilesBE.Models;

public class AssignmentApproval
{
    public int ApprovalID { get; set; }
    public int AssignmentID { get; set; }
    public string? ApproverRole { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Notes { get; set; }

    // Navigation property
    public MachineAssignment? MachineAssignment { get; set; }
}

