using Serilog;
using syncToServer;
using System.Text.Json;
using Microsoft.Data.SqlClient;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SyncToServer application");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Logging.ClearProviders();
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine("logs", "sync-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.Logging.AddSerilog();

    // Configure SyncSettings from appsettings.json
    builder.Services.Configure<SyncSettings>(
        builder.Configuration.GetSection(SyncSettings.SectionName));

    // SourcePath: nếu có giá trị thì dùng đường dẫn tuyệt đối đó (bất kỳ folder nào trên máy); nếu để trống thì tự tìm OneDrive + SourceSubfolder
    builder.Services.PostConfigure<SyncSettings>(settings =>
    {
        settings.SourcePath = (settings.SourcePath ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(settings.SourcePath))
        {
            string subfolder = string.IsNullOrWhiteSpace(settings.SourceSubfolder) ? "Backup_To_Server" : settings.SourceSubfolder.Trim();
            string detectedPath = DetectOneDrivePath(subfolder);
            if (!string.IsNullOrEmpty(detectedPath))
            {
                settings.SourcePath = detectedPath;
                Log.Information("Auto-detected source path (OneDrive): {SourcePath}", detectedPath);
            }
            else
            {
                Log.Warning("SourcePath trống và không tìm thấy OneDrive. Hãy chỉ định đường dẫn tuyệt đối trong appsettings.json (vd: \"SourcePath\": \"C:\\\\Backup\").");
            }
        }
        else
        {
            Log.Information("Using configured source path: {SourcePath}", settings.SourcePath);
        }
    });

    // DestServer: ưu tiên đọc file-storage-path từ DB, parse để lấy share name và subfolder (p-TK)
    builder.Services.PostConfigure<SyncSettings>(settings =>
    {
        bool destServerFromDb = false;
        if (!string.IsNullOrWhiteSpace(settings.DatabaseConnectionString) && !string.IsNullOrWhiteSpace(settings.ServerIp))
        {
            string? fileStoragePath = GetFileStoragePathFromDatabase(settings.DatabaseConnectionString);
            if (!string.IsNullOrEmpty(fileStoragePath))
            {
                var (shareName, subfolder) = ParseFileStoragePath(fileStoragePath);
                if (!string.IsNullOrEmpty(shareName))
                {
                    settings.DestServer = $"\\\\{settings.ServerIp}\\{shareName}";
                    settings.DestSubfolder = subfolder ?? string.Empty;
                    destServerFromDb = true;
                    Log.Information("DestServer set from database: '{FileStoragePath}' -> Share: '{DestServer}', Subfolder: '{DestSubfolder}'", 
                        fileStoragePath, settings.DestServer, settings.DestSubfolder);
                }
            }
        }
        
        if (string.IsNullOrWhiteSpace(settings.DestServer))
        {
            Log.Warning("DestServer chưa được cấu hình. Hãy đặt DestServer trong appsettings.json hoặc cấu hình DatabaseConnectionString + ServerIp để tự động lấy từ DB.");
        }
        else if (!destServerFromDb)
        {
            Log.Information("Using configured DestServer from appsettings: {DestServer}", settings.DestServer);
        }
    });

    // Register services
    builder.Services.AddSingleton<NetworkShareAccess>();
    builder.Services.AddScoped<SyncService>();
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    Log.Information("Application configured successfully");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Automatically detects the source path: prefers folder (e.g. Backup_To_Server) inside OneDrive.
/// Searches OneDrive root and one level deep (e.g. OneDrive\Chinh - CONG TY...\Backup_To_Server).
/// </summary>
static string DetectOneDrivePath(string backupFolderName = "Backup_To_Server")
{
    // #region agent log
    try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "DetectOneDrivePath", message = "Function entry", data = new { backupFolderName }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
    // #endregion

    try
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "DetectOneDrivePath", message = "UserProfile path", data = new { userProfile }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        // Get ALL OneDrive folders (not just the first one)
        var allOneDriveDirs = new List<string>();
        var patterns = new[]
        {
            Path.Combine(userProfile, "OneDrive - Thibidi"),
            Path.Combine(userProfile, "OneDrive")
        };
        foreach (var pattern in patterns)
        {
            bool exists = Directory.Exists(pattern);
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "DetectOneDrivePath", message = "Checking pattern", data = new { pattern, exists }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            if (exists)
            {
                allOneDriveDirs.Add(pattern);
            }
        }
        // Also search for all OneDrive* folders
        var wildcardDirs = Directory.GetDirectories(userProfile, "OneDrive*", SearchOption.TopDirectoryOnly);
        foreach (var dir in wildcardDirs)
        {
            if (!allOneDriveDirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
            {
                allOneDriveDirs.Add(dir);
            }
        }
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "DetectOneDrivePath", message = "All OneDrive folders found", data = new { allOneDriveDirs }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        // Search Backup_To_Server in ALL OneDrive folders
        foreach (var oneDriveRoot in allOneDriveDirs)
        {
            Log.Information("Searching in OneDrive: {OneDriveRoot}", oneDriveRoot);
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "DetectOneDrivePath", message = "Searching in OneDrive folder", data = new { oneDriveRoot }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion

            // Prefer Backup_To_Server: first directly under OneDrive root
            var backupAtRoot = Path.Combine(oneDriveRoot, backupFolderName);
            bool existsAtRoot = Directory.Exists(backupAtRoot);
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "DetectOneDrivePath", message = "Check backupAtRoot", data = new { backupAtRoot, existsAtRoot }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            if (existsAtRoot)
            {
                Log.Information("Found {BackupFolder} directly under OneDrive root: {Path}", backupFolderName, backupAtRoot);
                // #region agent log
                try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2", location = "DetectOneDrivePath", message = "Found at root", data = new { path = backupAtRoot }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return backupAtRoot;
            }

            // Recursively search for Backup_To_Server in all subdirectories (max depth: 5 levels)
            string? foundPath = SearchRecursive(oneDriveRoot, backupFolderName, maxDepth: 5);
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H3", location = "DetectOneDrivePath", message = "Recursive search result", data = new { foundPath, oneDriveRoot }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            if (!string.IsNullOrEmpty(foundPath))
            {
                Log.Information("Found {BackupFolder} via recursive search: {Path}", backupFolderName, foundPath);
                return foundPath;
            }
        }

        if (allOneDriveDirs.Count == 0)
        {
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1", location = "DetectOneDrivePath", message = "No OneDrive root found", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            return string.Empty;
        }

        // If Backup_To_Server not found, create it in the first OneDrive root
        // This ensures the sync can proceed even if the folder doesn't exist yet
        var firstOneDriveRoot = allOneDriveDirs[0];
        var createPath = Path.Combine(firstOneDriveRoot, backupFolderName);
        try
        {
            if (!Directory.Exists(createPath))
            {
                Directory.CreateDirectory(createPath);
                Log.Information("Created backup folder '{BackupFolder}' at: {Path}", backupFolderName, createPath);
                // #region agent log
                try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "DetectOneDrivePath", message = "Created folder", data = new { path = createPath }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
            }
            return createPath;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create backup folder '{BackupFolder}' at: {Path}", backupFolderName, createPath);
            // #region agent log
            try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "DetectOneDrivePath", message = "Failed to create folder", data = new { path = createPath, error = ex.Message }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }

        Log.Warning("Backup folder '{BackupFolder}' not found in any OneDrive folder. Searched: {OneDriveFolders} and subdirectories (max depth 5).", backupFolderName, string.Join(", ", allOneDriveDirs));
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H3", location = "DetectOneDrivePath", message = "Not found - returning empty", data = new { backupFolderName, searchedFolders = allOneDriveDirs }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        return string.Empty;

    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error detecting OneDrive path");
    }

    return string.Empty;
}

/// <summary>
/// Recursively searches for a folder with the specified name within a directory tree.
/// </summary>
static string? SearchRecursive(string rootDir, string folderName, int maxDepth, int currentDepth = 0)
{
    // #region agent log
    try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H3", location = "SearchRecursive", message = "Entry", data = new { rootDir, folderName, maxDepth, currentDepth }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
    // #endregion

    if (currentDepth >= maxDepth)
    {
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2", location = "SearchRecursive", message = "Max depth reached", data = new { currentDepth, maxDepth }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        return null;
    }

    try
    {
        // Check if folder exists directly in current directory
        var candidate = Path.Combine(rootDir, folderName);
        bool exists = Directory.Exists(candidate);
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H4", location = "SearchRecursive", message = "Check candidate", data = new { candidate, exists, currentDepth }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        if (exists)
        {
            return candidate;
        }

        // Recursively search in subdirectories
        var subDirs = Directory.GetDirectories(rootDir);
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H2,H3", location = "SearchRecursive", message = "Subdirectories found", data = new { rootDir, subDirCount = subDirs.Length, subDirs = subDirs.ToArray(), currentDepth }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
        foreach (var subDir in subDirs)
        {
            var found = SearchRecursive(subDir, folderName, maxDepth, currentDepth + 1);
            if (!string.IsNullOrEmpty(found))
            {
                return found;
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error searching in directory: {Dir}", rootDir);
        // #region agent log
        try { File.AppendAllText(@"d:\Project\thibidi\quanlyfiles\quanlyfileFE\.cursor\debug.log", JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H3", location = "SearchRecursive", message = "Exception", data = new { rootDir, error = ex.Message, stackTrace = ex.StackTrace }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion
    }

    return null;
}

/// <summary>
/// Đọc file-storage-path từ database Settings table.
/// </summary>
static string? GetFileStoragePathFromDatabase(string connectionString)
{
    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(
            "SELECT Value FROM Settings WHERE [Key] = 'file-storage-path'",
            connection);
        var result = command.ExecuteScalar();
        return result?.ToString();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to read file-storage-path from database");
        return null;
    }
}

/// <summary>
/// Parse local path (C:\LOCALSITE\p-TK) để lấy share name (LOCALSITE) và subfolder (p-TK).
/// Trả về (shareName, subfolder). Nếu không có subfolder thì subfolder = null.
/// </summary>
static (string shareName, string? subfolder) ParseFileStoragePath(string localPath)
{
    try
    {
        // Parse local path: C:\LOCALSITE\p-TK -> share name = LOCALSITE, subfolder = p-TK
        // Hoặc M:\LOCALSITE\p-TK -> share name = LOCALSITE, subfolder = p-TK
        // Hoặc C:\LOCALSITE -> share name = LOCALSITE, subfolder = null
        localPath = localPath.Trim().TrimEnd('\\');
        var parts = localPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            // parts[0] = drive letter (C:, M:), parts[1] = share name (LOCALSITE)
            string shareName = parts[1];
            // Nếu có phần thứ 3 trở đi thì đó là subfolder (p-TK)
            string? subfolder = parts.Length >= 3 ? parts[2] : null;
            return (shareName, subfolder);
        }
        Log.Warning("Cannot parse local path: {LocalPath}", localPath);
        return (string.Empty, null);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error parsing file storage path: {LocalPath}", localPath);
        return (string.Empty, null);
    }
}
