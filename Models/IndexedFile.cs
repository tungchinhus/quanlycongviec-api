namespace quanlyfilesBE.Models;

/// <summary>
/// Bảng index file cho Tra Cứu Files (service tìm file). Python indexer ghi dữ liệu vào đây thay vì SQLite.
/// </summary>
public class IndexedFile
{
    public int Id { get; set; }
    /// <summary>Đường dẫn đầy đủ tới file (UNC hoặc local).</summary>
    public string FullPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Thư mục chứa file (để filter LIKE folder_path%).</summary>
    public string FolderPath { get; set; } = string.Empty;
    /// <summary>Phần mở rộng file, ví dụ .xlsx</summary>
    public string Ext { get; set; } = string.Empty;
    /// <summary>Tên file đã chuẩn hóa (bỏ dấu) để tìm kiếm.</summary>
    public string? NameNormalized { get; set; }
    /// <summary>Thời gian sửa file (Unix timestamp).</summary>
    public double Mtime { get; set; }
}
