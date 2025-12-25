namespace quanlyfilesBE.Models;

public class TechnicalSheet
{
    public string TBKT_ID { get; set; } = string.Empty;
    public int? Power_kVA { get; set; }
    public string? VoltageSpec { get; set; }
    public int? Phase { get; set; }
    public string? StandardCode { get; set; }
    public string? Proposer { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? DrawingDate { get; set; }
    public string? Notes { get; set; }
    public string? SalesOrder { get; set; } // SO (Nếu có)
    public DateTime? HandOverDate { get; set; } // Ngày bàn giao
    public DateTime? ArchivedDate { get; set; } // Ngày lưu trữ (NGÀY LƯU)
    public string? RequesterElectrical { get; set; } // KS Điện
    public string? RequesterMechanical { get; set; } // KS Cơ

    // Navigation properties
    public ICollection<MachineAssignment> MachineAssignments { get; set; } = new List<MachineAssignment>();
    public ICollection<TechnicalNotification> TechnicalNotifications { get; set; } = new List<TechnicalNotification>();
}

