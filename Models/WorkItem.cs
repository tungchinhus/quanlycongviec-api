namespace quanlyfilesBE.Models;

public class WorkItem
{
    public int WorkItemID { get; set; }
    public int AssignmentID { get; set; }
    public string? WorkType { get; set; }
    public string? PersonName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedFinish { get; set; }
    public DateTime? ActualFinish { get; set; }
    public bool? PersonConfirmation { get; set; }
    public string? Notes { get; set; }
    public string? File_ID { get; set; } // Comma-separated file IDs (e.g., "1,2,3")

    // Navigation property
    public MachineAssignment? MachineAssignment { get; set; }
}

