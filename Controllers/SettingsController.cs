using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;
using quanlyfilesBE.Helpers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController>? _logger;

    public SettingsController(
        IOptions<FileStorageOptions> fileStorageOptions,
        ApplicationDbContext context,
        ILogger<SettingsController>? logger = null)
    {
        _fileStorageOptions = fileStorageOptions.Value;
        _context = context;
        _logger = logger;
    }

    // Map ổ mạng (vd. M:) sang UNC (\\server\share) giống logic trong Python service.
    // Dùng cho SearchFileIndex để khi FE gửi M:\... vẫn khớp với FolderPath UNC trong bảng FileIndex.
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnection(
        string lpLocalName,
        StringBuilder lpRemoteName,
        ref int lpnLength);

    private static string? ResolveNetworkDriveToUnc(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        var normalized = rawPath.Trim().Replace('/', '\\');
        var root = Path.GetPathRoot(normalized);
        if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
            return null;

        var drive = root.Substring(0, 2); // "M:"
        var length = 512;
        var sb = new StringBuilder(length);
        const int ERROR_MORE_DATA = 234;
        const int NO_ERROR = 0;

        var result = WNetGetConnection(drive, sb, ref length);
        if (result == ERROR_MORE_DATA && length > 0)
        {
            sb = new StringBuilder(length);
            result = WNetGetConnection(drive, sb, ref length);
        }

        if (result != NO_ERROR)
            return null;

        var uncRoot = sb.ToString().Trim().TrimEnd('\\');
        if (string.IsNullOrWhiteSpace(uncRoot))
            return null;

        var rest = normalized.Substring(root.Length).TrimStart('\\');
        return string.IsNullOrEmpty(rest) ? uncRoot : $"{uncRoot}\\{rest}";
    }

    // GET: api/settings/sync-credentials
    [HttpGet("sync-credentials")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetSyncCredentials()
    {
        try
        {
            var usernameSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "sync-network-username");
            var passwordSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "sync-network-password");
            var domainSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "sync-network-domain");

            return Ok(new SyncNetworkCredentialsDto
            {
                NetworkUsername = usernameSetting?.Value ?? string.Empty,
                HasPassword = !string.IsNullOrEmpty(passwordSetting?.Value),
                Domain = domainSetting?.Value
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetSyncCredentials: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving sync credentials", message = ex.Message });
        }
    }

    // PUT: api/settings/sync-credentials
    [HttpPut("sync-credentials")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateSyncCredentials([FromBody] UpdateSyncNetworkCredentialsDto updateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(updateDto.NetworkUsername))
            {
                return BadRequest(new { error = "NetworkUsername is required" });
            }

            await UpsertSettingAsync("sync-network-username", updateDto.NetworkUsername.Trim(), "Sync client: network share username");
            await UpsertSettingAsync("sync-network-domain", (updateDto.Domain ?? string.Empty).Trim(), "Sync client: network share domain (optional)");
            if (updateDto.NetworkPassword != null)
            {
                await UpsertSettingAsync("sync-network-password", updateDto.NetworkPassword, "Sync client: network share password (stored in DB, not in appsettings)");
            }

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Sync network credentials updated (Username: {Username})", updateDto.NetworkUsername);
            return Ok(new { message = "Sync credentials updated successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateSyncCredentials: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating sync credentials", message = ex.Message });
        }
    }

    private async Task UpsertSettingAsync(string key, string value, string description)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedAt = DateTimeHelper.NowVietnam(),
                UpdatedAt = DateTimeHelper.NowVietnam()
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTimeHelper.NowVietnam();
        }
    }

    // GET: api/settings/sync-interval
    [HttpGet("sync-interval")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetSyncInterval()
    {
        try
        {
            var syncIntervalSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "sync-interval-minutes");

            var syncIntervalMinutes = 2; // Default: 2 minutes
            if (syncIntervalSetting != null)
            {
                int.TryParse(syncIntervalSetting.Value, out syncIntervalMinutes);
                if (syncIntervalMinutes <= 0)
                {
                    syncIntervalMinutes = 2;
                }
            }

            return Ok(new { syncIntervalMinutes });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetSyncInterval: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving sync interval", message = ex.Message });
        }
    }

    // PUT: api/settings/sync-interval
    [HttpPut("sync-interval")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateSyncInterval([FromBody] UpdateSyncIntervalDto updateDto)
    {
        try
        {
            if (updateDto.SyncIntervalMinutes <= 0)
            {
                return BadRequest(new { error = "SyncIntervalMinutes must be greater than 0" });
            }

            var syncIntervalSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "sync-interval-minutes");

            if (syncIntervalSetting == null)
            {
                syncIntervalSetting = new Setting
                {
                    Key = "sync-interval-minutes",
                    Value = updateDto.SyncIntervalMinutes.ToString(),
                    Description = "Interval (minutes) between sync runs from client PCs to server.",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(syncIntervalSetting);
            }
            else
            {
                syncIntervalSetting.Value = updateDto.SyncIntervalMinutes.ToString();
                syncIntervalSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();

            return Ok(new { syncIntervalMinutes = updateDto.SyncIntervalMinutes });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateSyncInterval: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating sync interval", message = ex.Message });
        }
    }

    // GET: api/settings
    [HttpGet]
    [Authorize(Roles = "Administrator,Admin")]
    public IActionResult GetAllSettings()
    {
        try
        {
            var settings = new List<SettingDto>
            {
                new SettingDto
                {
                    SettingId = 1,
                    Key = "file-storage-path",
                    Value = _fileStorageOptions.Path,
                    Description = "File storage path on the server (configured in appsettings.json)",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                }
            };

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllSettings: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving settings", message = ex.Message });
        }
    }

    // GET: api/settings/file-storage-path
    [HttpGet("file-storage-path")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetFileStoragePath()
    {
        try
        {
            // Check database first, then fall back to appsettings.json
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "file-storage-path");
            
            var fileStoragePath = pathSetting != null 
                ? pathSetting.Value 
                : _fileStorageOptions.Path;

            return Ok(new { fileStoragePath });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetFileStoragePath: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving file storage path", message = ex.Message });
        }
    }

    // PUT: api/settings/file-storage-path
    [HttpPut("file-storage-path")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateFileStoragePath([FromBody] UpdateFileStoragePathDto updateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(updateDto.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            var trimmedPath = updateDto.Path.Trim();

            // Validate path
            var validationResult = ValidatePathInternal(trimmedPath);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            // Store in database (this overrides appsettings.json)
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "file-storage-path");

            if (pathSetting == null)
            {
                // Create new setting
                pathSetting = new Setting
                {
                    Key = "file-storage-path",
                    Value = trimmedPath,
                    Description = "File storage path on the server (overrides appsettings.json)",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(pathSetting);
            }
            else
            {
                // Update existing setting
                pathSetting.Value = trimmedPath;
                pathSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();

            _logger?.LogInformation("File storage path updated to: {Path}", trimmedPath);

            return Ok(new { fileStoragePath = trimmedPath, message = "File storage path updated successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateFileStoragePath: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error processing file storage path update request", message = ex.Message });
        }
    }

    // POST: api/settings/validate-path
    [HttpPost("validate-path")]
    [Authorize(Roles = "Administrator,Admin")]
    public IActionResult ValidatePath([FromBody] ValidatePathDto validateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(validateDto.Path))
            {
                return BadRequest(new { 
                    valid = false, 
                    message = "Path is required" 
                });
            }

            var validationResult = ValidatePathInternal(validateDto.Path);
            // Map to frontend expected format
            return Ok(new { 
                valid = validationResult.IsValid, 
                message = validationResult.ErrorMessage 
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ValidatePath: {Message}", ex.Message);
            return StatusCode(500, new { 
                valid = false, 
                message = $"Error validating path: {ex.Message}" 
            });
        }
    }

    private ValidatePathResponseDto ValidatePathInternal(string path)
    {
        try
        {
            // Trim whitespace
            path = path.Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path cannot be empty"
                };
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path contains invalid characters"
                };
            }

            // Check if path is too long (Windows max path is 260 chars, but we'll use a reasonable limit)
            if (path.Length > 500)
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path is too long (maximum 500 characters)"
                };
            }

            // Try to create a DirectoryInfo to validate the path format
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                // If we can create DirectoryInfo without exception, the path format is valid
                // Note: This doesn't check if the directory actually exists, just if the path is well-formed
            }
            catch (ArgumentException)
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path format is invalid"
                };
            }
            catch (PathTooLongException)
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path is too long"
                };
            }

            // Additional validation: check if path is absolute (recommended for server storage)
            if (!Path.IsPathRooted(path))
            {
                return new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path must be an absolute path (e.g., C:\\Files or /var/files)"
                };
            }

            return new ValidatePathResponseDto
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            return new ValidatePathResponseDto
            {
                IsValid = false,
                ErrorMessage = $"Error validating path: {ex.Message}"
            };
        }
    }

    // GET: api/settings/all
    [HttpGet("all")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetAllSystemSettings()
    {
        try
        {
            // Get file storage path from database first, then fall back to appsettings.json
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "file-storage-path");
            
            var fileStoragePath = pathSetting != null 
                ? pathSetting.Value 
                : _fileStorageOptions.Path;

            // Get notification setting from database
            var notificationSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
            
            var sendEmailNotifications = true; // Default to true
            if (notificationSetting != null)
            {
                bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
            }

            // Get warning days settings from database
            var designerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "designer-warning-days");
            var reviewerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "reviewer-warning-days");
            
            var designerWarningDays = 2; // Default: 2 days
            var reviewerWarningDays = 1; // Default: 1 day
            if (designerWarningDaysSetting != null)
            {
                int.TryParse(designerWarningDaysSetting.Value, out designerWarningDays);
            }
            if (reviewerWarningDaysSetting != null)
            {
                int.TryParse(reviewerWarningDaysSetting.Value, out reviewerWarningDays);
            }

            // Get signature storage path from database
            var signaturePathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "signature-storage-path");
            
            var signatureStoragePath = signaturePathSetting != null 
                ? signaturePathSetting.Value 
                : Path.Combine(Directory.GetCurrentDirectory(), "signatures"); // Default fallback

            // Get sync interval (minutes) from database
            var syncIntervalSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "sync-interval-minutes");

            var syncIntervalMinutes = 2; // Default: 2 minutes
            if (syncIntervalSetting != null)
            {
                int.TryParse(syncIntervalSetting.Value, out syncIntervalMinutes);
                if (syncIntervalMinutes <= 0)
                {
                    syncIntervalMinutes = 2;
                }
            }

            // INDEX_ROOTS: đường dẫn ổ mạng (Tra Cứu Files + sync service dùng chung)
            var indexRootsSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "INDEX_ROOTS");
            var indexRoots = indexRootsSetting?.Value?.Trim() ?? string.Empty;

            // indexer-scheduled-time: giờ chạy indexer trong ngày (HH:mm). Mặc định 02:00 nếu chưa có value.
            var indexerScheduledTimeSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "indexer-scheduled-time");
            var indexerScheduledTime = indexerScheduledTimeSetting?.Value?.Trim();
            if (string.IsNullOrEmpty(indexerScheduledTime))
                indexerScheduledTime = "02:00";

            var settings = new SystemSettingsDto
            {
                FileStoragePath = fileStoragePath,
                SignatureStoragePath = signatureStoragePath,
                IndexRoots = indexRoots,
                SendEmailNotifications = sendEmailNotifications,
                DesignerWarningDays = designerWarningDays,
                ReviewerWarningDays = reviewerWarningDays,
                SyncIntervalMinutes = syncIntervalMinutes,
                IndexerScheduledTime = indexerScheduledTime
            };

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllSystemSettings: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving system settings", message = ex.Message });
        }
    }

    // GET: api/settings/notification-preference
    [HttpGet("notification-preference")]
    [Authorize]
    public async Task<IActionResult> GetNotificationPreference()
    {
        try
        {
            var notificationSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
            
            var sendEmailNotifications = true; // Default to true
            if (notificationSetting != null)
            {
                bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
            }

            return Ok(new { sendEmailNotifications });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetNotificationPreference: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving notification preference", message = ex.Message });
        }
    }

    // PUT: api/settings/notification-preference
    [HttpPut("notification-preference")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateNotificationPreference([FromBody] UpdateNotificationSettingsDto updateDto)
    {
        try
        {
            var notificationSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");

            if (notificationSetting == null)
            {
                // Create new setting
                notificationSetting = new Setting
                {
                    Key = "send-email-notifications",
                    Value = updateDto.SendEmailNotifications.ToString(),
                    Description = "If true, send email notifications. If false, show notification badge on bell icon.",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(notificationSetting);
            }
            else
            {
                // Update existing setting
                notificationSetting.Value = updateDto.SendEmailNotifications.ToString();
                notificationSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();

            return Ok(new { sendEmailNotifications = updateDto.SendEmailNotifications });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateNotificationPreference: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating notification preference", message = ex.Message });
        }
    }

    // GET: api/settings/warning-days
    [HttpGet("warning-days")]
    [Authorize]
    public async Task<IActionResult> GetWarningDays()
    {
        try
        {
            var designerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "designer-warning-days");
            var reviewerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "reviewer-warning-days");
            
            var designerWarningDays = 2; // Default: 2 days
            var reviewerWarningDays = 1; // Default: 1 day
            if (designerWarningDaysSetting != null)
            {
                int.TryParse(designerWarningDaysSetting.Value, out designerWarningDays);
            }
            if (reviewerWarningDaysSetting != null)
            {
                int.TryParse(reviewerWarningDaysSetting.Value, out reviewerWarningDays);
            }

            return Ok(new { designerWarningDays, reviewerWarningDays });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetWarningDays: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving warning days", message = ex.Message });
        }
    }

    // GET: api/settings/signature-storage-path
    [HttpGet("signature-storage-path")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetSignatureStoragePath()
    {
        try
        {
            // Check database first, then fall back to default
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "signature-storage-path");
            
            var signatureStoragePath = pathSetting != null 
                ? pathSetting.Value 
                : Path.Combine(Directory.GetCurrentDirectory(), "signatures");

            return Ok(new { signatureStoragePath });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetSignatureStoragePath: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving signature storage path", message = ex.Message });
        }
    }

    // PUT: api/settings/signature-storage-path
    [HttpPut("signature-storage-path")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateSignatureStoragePath([FromBody] UpdateFileStoragePathDto updateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(updateDto.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            var trimmedPath = updateDto.Path.Trim();

            // Validate path
            var validationResult = ValidatePathInternal(trimmedPath);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            // Store in database
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "signature-storage-path");

            if (pathSetting == null)
            {
                // Create new setting
                pathSetting = new Setting
                {
                    Key = "signature-storage-path",
                    Value = trimmedPath,
                    Description = "Signature storage path on the server",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(pathSetting);
            }
            else
            {
                // Update existing setting
                pathSetting.Value = trimmedPath;
                pathSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Signature storage path updated to: {Path}", trimmedPath);

            return Ok(new { signatureStoragePath = trimmedPath, message = "Signature storage path updated successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateSignatureStoragePath: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error processing signature storage path update request", message = ex.Message });
        }
    }

    // GET: api/settings/index-roots
    [HttpGet("index-roots")]
    [Authorize]
    public async Task<IActionResult> GetIndexRoots()
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "INDEX_ROOTS");
            var indexRoots = setting?.Value?.Trim() ?? string.Empty;
            return Ok(new { indexRoots });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetIndexRoots: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving index roots", message = ex.Message });
        }
    }

    // GET: api/settings/search-file-index
    // Tra Cứu Files: tìm trong bảng FileIndex (SQL Server). Python service gọi qua HTTP, client không cần cài ODBC.
    [HttpGet("search-file-index")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchFileIndex(
        [FromQuery] string folderPath,
        [FromQuery] string q,
        [FromQuery] string? ext = null,
        [FromQuery] int maxResults = 500)
    {
        try
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp) && remoteIp != "127.0.0.1" && remoteIp != "::1" && !remoteIp.StartsWith("::ffff:127.0.0.1"))
            {
                _logger?.LogWarning("SearchFileIndex called from non-localhost: {IP}", remoteIp);
                return StatusCode(403, new { error = "Access denied. Only localhost allowed." });
            }
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return Ok(new { results = Array.Empty<object>(), usedIndex = true });
            }
            var folderPrefix = folderPath.Trim().Replace('/', '\\').TrimEnd('\\');
            var prefixes = new List<string>();
            if (!string.IsNullOrEmpty(folderPrefix))
                prefixes.Add(folderPrefix);
            var uncPrefix = ResolveNetworkDriveToUnc(folderPrefix);
            if (!string.IsNullOrEmpty(uncPrefix)
                && !prefixes.Contains(uncPrefix, StringComparer.OrdinalIgnoreCase))
            {
                prefixes.Add(uncPrefix.TrimEnd('\\'));
            }

            var extList = string.IsNullOrWhiteSpace(ext)
                ? new List<string>()
                : ext.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().ToLowerInvariant()).ToList();
            var keywords = string.IsNullOrWhiteSpace(q)
                ? new List<string>()
                : q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim().ToLowerInvariant())
                    .Where(w => w.Length > 0)
                    .ToList();
            const int scanCap = 4000;
            // 1) Một query: TOP N theo FolderPath + Mtime (dùng index, có thể covering), 1 round-trip.
            //    Hỗ trợ cả đường dẫn drive letter (M:\...) và UNC (\\server\share\...).
            IQueryable<IndexedFile> query = _context.FileIndex.AsNoTracking();
            if (prefixes.Count == 1)
            {
                var p = prefixes[0];
                query = query.Where(f => f.FolderPath == p || f.FolderPath.StartsWith(p + "\\"));
            }
            else if (prefixes.Count >= 2)
            {
                var p0 = prefixes[0];
                var p1 = prefixes[1];
                query = query.Where(f =>
                    f.FolderPath == p0 || f.FolderPath.StartsWith(p0 + "\\") ||
                    f.FolderPath == p1 || f.FolderPath.StartsWith(p1 + "\\"));
            }

            var candidates = await query
                .OrderByDescending(f => f.Mtime)
                .Take(scanCap)
                .Select(f => new { f.Name, f.FullPath, f.Ext, f.NameNormalized })
                .ToListAsync();
            // 2) Lọc theo ext + keyword trong memory (nhanh, không quét thêm DB).
            var filtered = candidates.AsEnumerable();
            if (extList.Count > 0)
                filtered = filtered.Where(f => extList.Contains((f.Ext ?? "").ToLowerInvariant()));
            foreach (var kw in keywords)
            {
                var k = kw;
                filtered = filtered.Where(f => f.NameNormalized != null && f.NameNormalized.Contains(k));
            }
            var results = filtered
                .Take(Math.Max(1, maxResults))
                .Select(f => new { name = f.Name, fullPath = f.FullPath })
                .ToList();
            return Ok(new { results, usedIndex = true });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in SearchFileIndex: {Message}", ex.Message);
            return StatusCode(500, new { results = Array.Empty<object>(), error = ex.Message });
        }
    }

    // GET: api/settings/indexer-sqlserver-connection-string
    // Cho phép Python service (chạy cùng server) lấy connection string để lưu index vào SQL Server
    [HttpGet("indexer-sqlserver-connection-string")]
    [AllowAnonymous] // Cho phép Python service gọi không cần auth (chạy cùng server)
    public IActionResult GetIndexerSqlServerConnectionString()
    {
        try
        {
            // Chỉ cho phép từ localhost để bảo mật
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp) && remoteIp != "127.0.0.1" && remoteIp != "::1" && !remoteIp.StartsWith("::ffff:127.0.0.1"))
            {
                _logger?.LogWarning("GetIndexerSqlServerConnectionString called from non-localhost: {IP}", remoteIp);
                return StatusCode(403, new { error = "Access denied. Only localhost allowed." });
            }

            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return NotFound(new { error = "Connection string not found" });
            }

            return Ok(new { connectionString });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetIndexerSqlServerConnectionString: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving connection string", message = ex.Message });
        }
    }

    // GET: api/settings/indexer-scheduled-time
    [HttpGet("indexer-scheduled-time")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetIndexerScheduledTime()
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "indexer-scheduled-time");
            var indexerScheduledTime = setting?.Value?.Trim();
            if (string.IsNullOrEmpty(indexerScheduledTime))
                indexerScheduledTime = "02:00";
            return Ok(new { indexerScheduledTime });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetIndexerScheduledTime: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving indexer scheduled time", message = ex.Message });
        }
    }

    // PUT: api/settings/indexer-scheduled-time
    [HttpPut("indexer-scheduled-time")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateIndexerScheduledTime([FromBody] UpdateIndexerScheduledTimeDto updateDto)
    {
        try
        {
            var value = (updateDto?.IndexerScheduledTime ?? string.Empty).Trim();
            // Cho phép để trống (tắt chạy theo giờ). Validate format HH:mm nếu có giá trị
            if (!string.IsNullOrEmpty(value))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    return BadRequest(new { error = "IndexerScheduledTime must be HH:mm (e.g. 02:00)" });
                }
            }

            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "indexer-scheduled-time");
            var valueToStore = string.IsNullOrEmpty(value) ? "" : value; // Không lưu null xuống DB
            if (setting == null)
            {
                setting = new Setting
                {
                    Key = "indexer-scheduled-time",
                    Value = valueToStore,
                    Description = "Giờ trong ngày chạy indexer tìm file (HH:mm). Service Python đọc qua API hoặc env.",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = valueToStore;
                setting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("indexer-scheduled-time updated to: {Value}", valueToStore);
            return Ok(new { indexerScheduledTime = valueToStore, message = "Indexer scheduled time updated successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateIndexerScheduledTime: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating indexer scheduled time", message = ex.Message });
        }
    }

    // PUT: api/settings/index-roots
    [HttpPut("index-roots")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateIndexRoots([FromBody] UpdateFileStoragePathDto updateDto)
    {
        try
        {
            var value = (updateDto.Path ?? string.Empty).Trim();
            // Cho phép để trống (optional)
            var validationResult = string.IsNullOrEmpty(value) ? new ValidatePathResponseDto { IsValid = true } : ValidatePathInternal(value);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "INDEX_ROOTS");
            if (setting == null)
            {
                setting = new Setting
                {
                    Key = "INDEX_ROOTS",
                    Value = value,
                    Description = "Đường dẫn ổ mạng (Tra Cứu Files + sync service dùng chung)",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("INDEX_ROOTS updated to: {Value}", value);
            return Ok(new { indexRoots = value, message = "INDEX_ROOTS updated successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateIndexRoots: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating INDEX_ROOTS", message = ex.Message });
        }
    }

    // PUT: api/settings/warning-days
    [HttpPut("warning-days")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateWarningDays([FromBody] UpdateWarningDaysSettingsDto updateDto)
    {
        try
        {
            // Validate values
            if (updateDto.DesignerWarningDays < 0 || updateDto.ReviewerWarningDays < 0)
            {
                return BadRequest(new { error = "Warning days must be non-negative" });
            }

            // Update or create designer warning days setting
            var designerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "designer-warning-days");

            if (designerWarningDaysSetting == null)
            {
                designerWarningDaysSetting = new Setting
                {
                    Key = "designer-warning-days",
                    Value = updateDto.DesignerWarningDays.ToString(),
                    Description = "Number of days before expected finish date to show warning for designers",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(designerWarningDaysSetting);
            }
            else
            {
                designerWarningDaysSetting.Value = updateDto.DesignerWarningDays.ToString();
                designerWarningDaysSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            // Update or create reviewer warning days setting
            var reviewerWarningDaysSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "reviewer-warning-days");

            if (reviewerWarningDaysSetting == null)
            {
                reviewerWarningDaysSetting = new Setting
                {
                    Key = "reviewer-warning-days",
                    Value = updateDto.ReviewerWarningDays.ToString(),
                    Description = "Number of days before confirmation to show warning for reviewers",
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    UpdatedAt = DateTimeHelper.NowVietnam()
                };
                _context.Settings.Add(reviewerWarningDaysSetting);
            }
            else
            {
                reviewerWarningDaysSetting.Value = updateDto.ReviewerWarningDays.ToString();
                reviewerWarningDaysSetting.UpdatedAt = DateTimeHelper.NowVietnam();
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                designerWarningDays = updateDto.DesignerWarningDays, 
                reviewerWarningDays = updateDto.ReviewerWarningDays 
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateWarningDays: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating warning days", message = ex.Message });
        }
    }
}

