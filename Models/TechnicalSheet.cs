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

    // Approval workflow fields - ManagerL1 approval (first level)
    public string? ManagerL1ApprovalStatus { get; set; } // Pending, Approved, Rejected
    public string? ManagerL1ApproverFirebaseUID { get; set; }
    public DateTime? ManagerL1ApprovalDate { get; set; }
    public string? ManagerL1ApprovalNotes { get; set; }

    // Approval workflow fields - Manager approval (final level)
    public string? ManagerApprovalStatus { get; set; } // Pending, Approved, Rejected
    public string? ManagerApproverFirebaseUID { get; set; }
    public DateTime? ManagerApprovalDate { get; set; }
    public string? ManagerApprovalNotes { get; set; }

    // Navigation properties
    public ICollection<MachineAssignment> MachineAssignments { get; set; } = new List<MachineAssignment>();
    public ICollection<TechnicalNotification> TechnicalNotifications { get; set; } = new List<TechnicalNotification>();
}

