namespace syncToServer;

public class SyncSettings
{
    public const string SectionName = "SyncSettings";

    /// <summary>Đường dẫn tuyệt đối đến thư mục nguồn trên máy (vd: C:\Backup, D:\Data\Sync). Có thể là bất kỳ folder nào, không bắt buộc OneDrive. Để trống thì tự tìm OneDrive + SourceSubfolder.</summary>
    public string SourcePath { get; set; } = string.Empty;
    /// <summary>Chỉ dùng khi SourcePath để trống: tên thư mục cần tìm trong OneDrive (mặc định: Backup_To_Server).</summary>
    public string SourceSubfolder { get; set; } = "Backup_To_Server";
    public string DestServer { get; set; } = string.Empty;
    /// <summary>Subfolder trong DestServer (vd: p-TK). Được lấy từ file-storage-path trong DB. Folder user sẽ được tạo trong DestServer\DestSubfolder\username.</summary>
    public string DestSubfolder { get; set; } = string.Empty;
    /// <summary>IP hoặc hostname của server (vd: 172.20.115.40). Dùng để convert local path từ DB sang UNC path.</summary>
    public string ServerIp { get; set; } = string.Empty;
    /// <summary>Connection string để đọc database (nếu để trống thì dùng DestServer từ config).</summary>
    public string? DatabaseConnectionString { get; set; }
    public NetworkCredentials NetworkCredentials { get; set; } = new();
    public int IntervalMinutes { get; set; } = 15;
}

public class NetworkCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
}
