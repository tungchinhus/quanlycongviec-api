using System.ComponentModel.DataAnnotations;

namespace quanlyfilesBE.DTOs;

public class ExcelExportRequest
{
    [Required]
    public List<StatisticsDataDto> StatisticsData { get; set; } = new();
}

public class StatisticsDataDto
{
    public string? CongSuat { get; set; }
    public string? Tbkt { get; set; }
    public double? SoMau { get; set; }
    
    // Pk H1
    public double? PkH1Max { get; set; }
    public double? PkH1TB { get; set; }
    public double? PkH1Min { get; set; }
    public double? PkH1Delta { get; set; }
    
    // Pk H2
    public double? PkH2Max { get; set; }
    public double? PkH2TB { get; set; }
    public double? PkH2Min { get; set; }
    public double? PkH2Delta { get; set; }
    
    // Uk H1
    public double? UkH1Max { get; set; }
    public double? UkH1TB { get; set; }
    public double? UkH1Min { get; set; }
    public double? UkH1Delta { get; set; }
    
    // Uk H2
    public double? UkH2Max { get; set; }
    public double? UkH2TB { get; set; }
    public double? UkH2Min { get; set; }
    public double? UkH2Delta { get; set; }
}

