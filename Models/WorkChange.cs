namespace quanlyfilesBE.Models;

public class WorkChange
{
    public int ChangeID { get; set; }
    public int AssignmentID { get; set; }
    public string? ChangeType { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public MachineAssignment? MachineAssignment { get; set; }
}

