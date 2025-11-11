namespace quanlyfilesBE.Models;

public class TechnicalNotification
{
    public int NotificationID { get; set; }
    public string TBKT_ID { get; set; } = string.Empty;
    public string? DesignReason { get; set; }
    public string? TechnicalStatus { get; set; }
    public string? RoutDrawingCode { get; set; }
    public string? VoDrawingCode { get; set; }
    public decimal? VoLength { get; set; }
    public decimal? VoWidth { get; set; }
    public decimal? VoHeight { get; set; }
    public string? Accessories { get; set; }
    public string? MaterialUsage { get; set; }
    public string? TechnicalNotes { get; set; }
    public string? Signer_Proposal { get; set; }
    public string? Signer_Designer { get; set; }
    public string? Signer_Approver { get; set; }
    public DateTime? SignDate { get; set; }

    // Navigation property
    public TechnicalSheet? TechnicalSheet { get; set; }
}

