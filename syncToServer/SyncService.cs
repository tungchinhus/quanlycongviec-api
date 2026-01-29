using Microsoft.Extensions.Options;

namespace syncToServer;

/// <summary>
/// Service responsible for synchronizing files from source to destination.
/// Implements smart copy logic: only copies new or modified files.
/// </summary>
public class SyncService
{
    private readonly ILogger<SyncService> _logger;
    private readonly SyncSettings _settings;
    private readonly NetworkShareAccess _networkShare;

    public SyncService(
        ILogger<SyncService> logger,
        IOptions<SyncSettings> settings,
        NetworkShareAccess networkShare)
    {
        _logger = logger;
        _settings = settings.Value;
        _networkShare = networkShare;
    }

    /// <summary>
    /// Performs the synchronization operation.
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        try
        {
            // Validate source path
            if (string.IsNullOrWhiteSpace(_settings.SourcePath))
            {
                _logger.LogError("SourcePath is not configured");
                return false;
            }

            if (!Directory.Exists(_settings.SourcePath))
            {
                _logger.LogError("Source path does not exist: {SourcePath}", _settings.SourcePath);
                return false;
            }

            // Connect to network share
            _logger.LogInformation("Connecting to network share: {DestServer}", _settings.DestServer);
            bool connected = _networkShare.Connect(
                _settings.DestServer,
                _settings.NetworkCredentials.Username,
                _settings.NetworkCredentials.Password,
                _settings.NetworkCredentials.Domain ?? "",
                out int lastError);

            if (!connected)
            {
                string msg = NetworkShareAccess.GetErrorMessage(lastError);
                _logger.LogError(
                    "Failed to connect to network share: {DestServer}. {ErrorMsg} (Code: {ErrorCode})",
                    _settings.DestServer, msg, lastError);
                return false;
            }

            _logger.LogInformation("Successfully connected to network share");

            // Mỗi máy user có 1 folder cố định trên server, đặt tên theo user Windows (vd: chinh.dvt)
            // Folder user được tạo trong DestServer\DestSubfolder\username (vd: \\server\LOCALSITE\p-TK\chinh.dvt)
            string userName = Environment.UserName ?? "Backup_To_Server";
            string destBase = _settings.DestServer.TrimEnd('\\');
            if (!string.IsNullOrWhiteSpace(_settings.DestSubfolder))
            {
                destBase = Path.Combine(destBase, _settings.DestSubfolder);
            }
            string destRoot = Path.Combine(destBase, userName);
            _logger.LogInformation("Sync destination (user folder): {DestRoot}", destRoot);

            if (!Directory.Exists(destRoot))
            {
                Directory.CreateDirectory(destRoot);
                _logger.LogInformation("Created user folder on server: {DestRoot}", destRoot);
            }

            // Perform sync (copy new/updated from source to dest)
            var stats = new SyncStats();
            await SyncDirectoryAsync(_settings.SourcePath, destRoot, stats);

            // Xóa trên server các file/thư mục không còn tồn tại ở nguồn (mirror delete)
            await RemoveDeletedFromDestAsync(_settings.SourcePath, destRoot, stats);

            _logger.LogInformation(
                "Sync completed. Copied: {Copied}, Skipped: {Skipped}, Deleted on server: {Deleted}, Errors: {Errors}",
                stats.FilesCopied, stats.FilesSkipped, stats.FilesOrDirsDeleted, stats.Errors);

            return stats.Errors == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronization");
            return false;
        }
    }

    /// <summary>
    /// Recursively synchronizes a directory.
    /// </summary>
    private async Task SyncDirectoryAsync(
        string sourceDir,
        string destDir,
        SyncStats stats)
    {
        try
        {
            // Get relative path from source root
            string relativePath = Path.GetRelativePath(_settings.SourcePath, sourceDir);
            string currentDestDir = string.IsNullOrEmpty(relativePath) || relativePath == "."
                ? destDir
                : Path.Combine(destDir, relativePath);

            // Ensure destination directory exists
            if (!Directory.Exists(currentDestDir))
            {
                Directory.CreateDirectory(currentDestDir);
                _logger.LogDebug("Created directory: {DestDir}", currentDestDir);
            }

            // Copy files in current directory
            var files = Directory.GetFiles(sourceDir);
            foreach (var sourceFile in files)
            {
                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(currentDestDir, fileName);

                if (await ShouldCopyFileAsync(sourceFile, destFile))
                {
                    if (await CopyFileWithRetryAsync(sourceFile, destFile))
                    {
                        stats.FilesCopied++;
                        _logger.LogInformation("Copied: {SourceFile} -> {DestFile}", sourceFile, destFile);
                    }
                    else
                    {
                        stats.Errors++;
                        _logger.LogWarning("Failed to copy: {SourceFile}", sourceFile);
                    }
                }
                else
                {
                    stats.FilesSkipped++;
                    _logger.LogDebug("Skipped (up to date): {SourceFile}", sourceFile);
                }
            }

            // Recursively process subdirectories
            var subDirs = Directory.GetDirectories(sourceDir);
            foreach (var subDir in subDirs)
            {
                await SyncDirectoryAsync(subDir, destDir, stats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing directory: {SourceDir}", sourceDir);
            stats.Errors++;
        }
    }

    /// <summary>
    /// Determines if a file should be copied based on existence and modification time.
    /// </summary>
    private async Task<bool> ShouldCopyFileAsync(string sourceFile, string destFile)
    {
        return await Task.Run(() =>
        {
            try
            {
                // If destination doesn't exist, copy it
                if (!File.Exists(destFile))
                {
                    return true;
                }

                // Compare LastWriteTime
                var sourceInfo = new FileInfo(sourceFile);
                var destInfo = new FileInfo(destFile);

                // Copy if source is newer
                return sourceInfo.LastWriteTime > destInfo.LastWriteTime;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking file: {SourceFile}", sourceFile);
                // If we can't check, try to copy anyway
                return true;
            }
        });
    }

    /// <summary>
    /// Copies a file with retry logic for locked files.
    /// </summary>
    private async Task<bool> CopyFileWithRetryAsync(string sourceFile, string destFile, int maxRetries = 3, int delayMs = 1000)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Ensure destination directory exists
                string? destDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Copy file
                File.Copy(sourceFile, destFile, overwrite: true);
                return true;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                // File might be locked (e.g., OneDrive sync in progress)
                _logger.LogWarning(
                    "File locked (attempt {Attempt}/{MaxRetries}): {SourceFile}. Retrying in {DelayMs}ms...",
                    attempt, maxRetries, sourceFile, delayMs);

                await Task.Delay(delayMs);
                // Exponential backoff
                delayMs *= 2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file (attempt {Attempt}/{MaxRetries}): {SourceFile}",
                    attempt, maxRetries, sourceFile);
                if (attempt == maxRetries)
                {
                    return false;
                }
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }

        return false;
    }

    /// <summary>
    /// Xóa trên server các file/thư mục không còn tồn tại ở nguồn (khi xóa ở máy cá nhân thì xóa tương ứng trên server).
    /// </summary>
    private async Task RemoveDeletedFromDestAsync(string sourceDir, string destDir, SyncStats stats)
    {
        await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(destDir))
                    return;

                // Xóa file trên server nếu không còn ở nguồn
                foreach (var destFile in Directory.GetFiles(destDir))
                {
                    string fileName = Path.GetFileName(destFile);
                    string sourceFile = Path.Combine(sourceDir, fileName);
                    if (!File.Exists(sourceFile))
                    {
                        try
                        {
                            File.Delete(destFile);
                            stats.FilesOrDirsDeleted++;
                            _logger.LogInformation("Deleted on server (removed locally): {DestFile}", destFile);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete on server: {DestFile}", destFile);
                            stats.Errors++;
                        }
                    }
                }

                // Thư mục con: nếu không còn ở nguồn thì xóa cả cây trên server; nếu còn thì đệ quy
                foreach (var destSubDir in Directory.GetDirectories(destDir))
                {
                    string subDirName = Path.GetFileName(destSubDir);
                    string sourceSubDir = Path.Combine(sourceDir, subDirName);
                    if (!Directory.Exists(sourceSubDir))
                    {
                        try
                        {
                            Directory.Delete(destSubDir, recursive: true);
                            stats.FilesOrDirsDeleted++;
                            _logger.LogInformation("Deleted folder on server (removed locally): {DestDir}", destSubDir);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete folder on server: {DestDir}", destSubDir);
                            stats.Errors++;
                        }
                    }
                    else
                    {
                        RemoveDeletedFromDestAsync(sourceSubDir, destSubDir, stats).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing deleted items from: {DestDir}", destDir);
                stats.Errors++;
            }
        });
    }

    /// <summary>
    /// Statistics tracking for sync operations.
    /// </summary>
    private class SyncStats
    {
        public int FilesCopied;
        public int FilesSkipped;
        public int FilesOrDirsDeleted;
        public int Errors;
    }
}
