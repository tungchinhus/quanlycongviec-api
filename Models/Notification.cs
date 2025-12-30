namespace quanlyfilesBE.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FirebaseUID or UserId
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, error, success
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntityType { get; set; } // e.g., "File", "WorkItem", "Assignment"
    public int? RelatedEntityId { get; set; } // ID of related entity
}

