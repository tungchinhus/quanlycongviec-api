namespace quanlyfilesBE.Models;

public class Folder
{
    public int Id { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public int? ParentFolderId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation property
    public Folder? ParentFolder { get; set; }
}

