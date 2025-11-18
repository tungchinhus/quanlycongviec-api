using System.ComponentModel.DataAnnotations;

namespace quanlyfilesBE.DTOs;

public class CreateFileItemDto
{
    public int? AssignmentID { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string FileType { get; set; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long FileSize { get; set; }

    [StringLength(100)]
    public string UploadedBy { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class UpdateFileItemDto
{
    [StringLength(255)]
    public string? FileName { get; set; }

    [StringLength(100)]
    public string? FileType { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

