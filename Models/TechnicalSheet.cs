namespace quanlyfilesBE.Models;

public class TechnicalSheet
{
    public string TBKT_ID { get; set; } = string.Empty;
    public decimal? Power_kVA { get; set; }
    public string? VoltageSpec { get; set; }
    public string? Phase { get; set; }
    public string? StandardCode { get; set; }
    public string? Proposer { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? DrawingDate { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<MachineAssignment> MachineAssignments { get; set; } = new List<MachineAssignment>();
    public ICollection<TechnicalNotification> TechnicalNotifications { get; set; } = new List<TechnicalNotification>();
}

