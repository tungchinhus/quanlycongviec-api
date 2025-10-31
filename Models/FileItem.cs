namespace quanlyfilesBE.Models;

public class FileItem
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? Description { get; set; }
}

