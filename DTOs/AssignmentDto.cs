using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace quanlyfilesBE.DTOs;

public class CreateMachineAssignmentDto
{
    [Required]
    [StringLength(50)]
    public string TBKT_ID { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string MachineName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? RequestDocument { get; set; } // ĐĐH/Giấy đề nghị

    [StringLength(1000)]
    public string? StandardRequirement { get; set; }

    [StringLength(1000)]
    public string? AdditionalRequest { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [StringLength(100)]
    public string? Designer { get; set; }

    [StringLength(100)]
    public string? TeamLeader { get; set; }

    [StringLength(4000)]
    [JsonPropertyName("filePaths")]
    public string? FilePath { get; set; }

    public int Status { get; set; } = 1; // 1: new, 2: đang xử lý, 3: hoàn thành

    // TechnicalSheet data
    public CreateTechnicalSheetDto? TechnicalSheet { get; set; }
}

public class CreateTechnicalSheetDto
{
    [Required]
    [StringLength(50)]
    public string TBKT_ID { get; set; } = string.Empty;

    public int? Power_kVA { get; set; }

    [StringLength(255)]
    public string? VoltageSpec { get; set; }

    public int? Phase { get; set; }

    [StringLength(100)]
    public string? StandardCode { get; set; }

    [StringLength(200)]
    public string? Proposer { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? DrawingDate { get; set; }

    [StringLength(100)]
    public string? SalesOrder { get; set; }

    public DateTime? HandOverDate { get; set; }

    public DateTime? ArchivedDate { get; set; }

    [StringLength(100)]
    public string? RequesterElectrical { get; set; }

    [StringLength(100)]
    public string? RequesterMechanical { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateMachineAssignmentDto
{
    [StringLength(50)]
    public string? TBKT_ID { get; set; }

    [StringLength(255)]
    public string? MachineName { get; set; }

    [StringLength(255)]
    public string? RequestDocument { get; set; } // ĐĐH/Giấy đề nghị

    [StringLength(1000)]
    public string? StandardRequirement { get; set; }

    [StringLength(1000)]
    public string? AdditionalRequest { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [StringLength(100)]
    public string? Designer { get; set; }

    [StringLength(100)]
    public string? TeamLeader { get; set; }

    [StringLength(4000)]
    [JsonPropertyName("filePaths")]
    public string? FilePath { get; set; }

    public int? Status { get; set; } // 1: new, 2: đang xử lý, 3: hoàn thành
}

public class MachineAssignmentDto
{
    public int AssignmentID { get; set; }
    
    [JsonPropertyName("tbkt_ID")]
    public string TBKT_ID { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    
    [JsonPropertyName("requestDocument")]
    public string? RequestDocument { get; set; } // ĐĐH/Giấy đề nghị
    
    public string? StandardRequirement { get; set; }
    public string? AdditionalRequest { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Designer { get; set; }
    public string? TeamLeader { get; set; }
    
    [JsonPropertyName("filePaths")]
    public string? FilePath { get; set; }
    
    public int Status { get; set; }
    public bool IsLocked { get; set; }
    public TechnicalSheetDto? TechnicalSheet { get; set; }
    public List<AssignmentApprovalDto>? AssignmentApprovals { get; set; }
    public List<WorkChangeDto>? WorkChanges { get; set; }
    public List<WorkItemDto>? WorkItems { get; set; }
}

public class TechnicalSheetDto
{
    [JsonPropertyName("tbkt_ID")]
    public string TBKT_ID { get; set; } = string.Empty;
    public int? Power_kVA { get; set; }
    public string? VoltageSpec { get; set; }
    public int? Phase { get; set; }
    public string? StandardCode { get; set; }
    [StringLength(200)]
    public string? Proposer { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? DrawingDate { get; set; }
    public string? Notes { get; set; }
    public string? SalesOrder { get; set; }
    public DateTime? HandOverDate { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public string? RequesterElectrical { get; set; }
    public string? RequesterMechanical { get; set; }
    
    // Approval workflow fields - ManagerL1 approval
    public string? ManagerL1ApprovalStatus { get; set; }
    public string? ManagerL1ApproverFirebaseUID { get; set; }
    public DateTime? ManagerL1ApprovalDate { get; set; }
    public string? ManagerL1ApprovalNotes { get; set; }
    
    // Approval workflow fields - Manager approval
    public string? ManagerApprovalStatus { get; set; }
    public string? ManagerApproverFirebaseUID { get; set; }
    public DateTime? ManagerApprovalDate { get; set; }
    public string? ManagerApprovalNotes { get; set; }
    
    // Lịch sử approvals từ bảng TechnicalSheetApproval (mới)
    public List<TechnicalSheetApprovalDto>? TechnicalSheetApprovals { get; set; }
}

public class AssignmentApprovalDto
{
    public int ApprovalID { get; set; }
    public int AssignmentID { get; set; }
    public string? ApproverRole { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Notes { get; set; }
}

public class TechnicalSheetApprovalDto
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
}

public class CreateAssignmentApprovalDto
{
    [Required]
    public int AssignmentID { get; set; }

    [StringLength(100)]
    public string? ApproverRole { get; set; }

    [StringLength(100)]
    public string? ApproverName { get; set; }

    public DateTime? ApprovalDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class WorkChangeDto
{
    public int ChangeID { get; set; }
    public int AssignmentID { get; set; }
    public string? ChangeType { get; set; }
    public string? Description { get; set; }
}

public class CreateWorkChangeDto
{
    [Required]
    public int AssignmentID { get; set; }

    [StringLength(100)]
    public string? ChangeType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class WorkItemDto
{
    public int WorkItemID { get; set; }
    public int AssignmentID { get; set; }
    public string? WorkType { get; set; }
    public string? PersonName { get; set; }
    public string? FullName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedFinish { get; set; }
    public DateTime? ActualFinish { get; set; }
    public bool? PersonConfirmation { get; set; }
    public string? Notes { get; set; }
    
    [JsonPropertyName("file_ID")]
    public string? File_ID { get; set; }
    
    public string? MachineName { get; set; }
    
    [JsonPropertyName("tbkt_ID")]
    public string? TBKT_ID { get; set; }
    
    public int? Power_kVA { get; set; }
    
    [JsonPropertyName("deliveryDate")]
    public DateTime? DeliveryDate { get; set; } // Ngày hoàn thành của TBKT tổng (từ MachineAssignment)
}

public class CreateWorkItemDto
{
    [Required]
    public int AssignmentID { get; set; }

    [StringLength(100)]
    public string? WorkType { get; set; }

    [StringLength(100)]
    public string? PersonName { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? StartDate { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? ExpectedFinish { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? ActualFinish { get; set; }
    
    public bool? PersonConfirmation { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateWorkItemDto
{
    [StringLength(100)]
    public string? WorkType { get; set; }

    [StringLength(100)]
    public string? PersonName { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? StartDate { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? ExpectedFinish { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? ActualFinish { get; set; }
    
    public bool? PersonConfirmation { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? File_ID { get; set; } // Comma-separated file IDs (e.g., "1,2,3")
}

public class WorkItemWithAssignmentDto
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
    public MachineAssignmentDto? Assignment { get; set; }
}

public class ApproveTechnicalSheetDto
{
    [Required]
    [StringLength(20)]
    public string ApprovalLevel { get; set; } = string.Empty; // "ManagerL1" or "Manager"

    [Required]
    [StringLength(20)]
    public string Action { get; set; } = string.Empty; // "approve" or "reject"

    [StringLength(1000)]
    public string? Notes { get; set; }
}

