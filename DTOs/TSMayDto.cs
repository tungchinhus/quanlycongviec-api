using System.ComponentModel.DataAnnotations;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.DTOs;

public class CreateTSMayDto
{
    public int? CongSuat { get; set; }

    [StringLength(50)]
    public string? SoMay { get; set; }

    [StringLength(50)]
    public string? SBB { get; set; }

    [StringLength(50)]
    public string? LSX { get; set; }

    [StringLength(50)]
    public string? TChuanLSX { get; set; }

    [StringLength(50)]
    public string? TBKT { get; set; }

    [StringLength(50)]
    public string? Po { get; set; }

    [StringLength(50)]
    public string? Io { get; set; }

    [StringLength(50)]
    public string? Pk75H1 { get; set; }

    [StringLength(50)]
    public string? Pk75H2 { get; set; }

    [StringLength(50)]
    public string? Uk75H1 { get; set; }

    [StringLength(50)]
    public string? Uk75H2 { get; set; }

    [StringLength(50)]
    public string? UdmHVH1 { get; set; }

    [StringLength(50)]
    public string? UdmHVH2 { get; set; }

    [StringLength(50)]
    public string? UdmLV { get; set; }

    [StringLength(1)]
    [RegularExpression("^[13]$", ErrorMessage = "Phase phải là '1' (1 pha) hoặc '3' (3 pha)")]
    public string? Phase { get; set; }
}

public class UpdateTSMayDto
{
    public int? CongSuat { get; set; }

    [StringLength(50)]
    public string? SoMay { get; set; }

    [StringLength(50)]
    public string? SBB { get; set; }

    [StringLength(50)]
    public string? LSX { get; set; }

    [StringLength(50)]
    public string? TChuanLSX { get; set; }

    [StringLength(50)]
    public string? TBKT { get; set; }

    [StringLength(50)]
    public string? Po { get; set; }

    [StringLength(50)]
    public string? Io { get; set; }

    [StringLength(50)]
    public string? Pk75H1 { get; set; }

    [StringLength(50)]
    public string? Pk75H2 { get; set; }

    [StringLength(50)]
    public string? Uk75H1 { get; set; }

    [StringLength(50)]
    public string? Uk75H2 { get; set; }

    [StringLength(50)]
    public string? UdmHVH1 { get; set; }

    [StringLength(50)]
    public string? UdmHVH2 { get; set; }

    [StringLength(50)]
    public string? UdmLV { get; set; }

    [StringLength(1)]
    [RegularExpression("^[13]$", ErrorMessage = "Phase phải là '1' (1 pha) hoặc '3' (3 pha)")]
    public string? Phase { get; set; }
}

public class BulkCreateTSMayDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Items array cannot be empty")]
    [MaxLength(50000, ErrorMessage = "Items array cannot exceed 50000 items")]
    public List<CreateTSMayDto> Items { get; set; } = new();
}

public class BulkCreateTSMayResponseDto
{
    public bool Success { get; set; }
    public int Total { get; set; }
    public int Created { get; set; }
    public int Failed { get; set; }
    public List<BulkCreateErrorDto>? Errors { get; set; }
}

public class BulkCreateErrorDto
{
    public int Index { get; set; }
    public CreateTSMayDto Data { get; set; } = new();
    public string Error { get; set; } = string.Empty;
}

public class SearchTSMayResponseDto
{
    public List<TSMay> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

