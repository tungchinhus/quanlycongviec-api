using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;
using System.IO;

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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(syncIntervalSetting);
            }
            else
            {
                syncIntervalSetting.Value = updateDto.SyncIntervalMinutes.ToString();
                syncIntervalSetting.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(pathSetting);
            }
            else
            {
                // Update existing setting
                pathSetting.Value = trimmedPath;
                pathSetting.UpdatedAt = DateTime.UtcNow;
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

            var settings = new SystemSettingsDto
            {
                FileStoragePath = fileStoragePath,
                SignatureStoragePath = signatureStoragePath,
                SendEmailNotifications = sendEmailNotifications,
                DesignerWarningDays = designerWarningDays,
                ReviewerWarningDays = reviewerWarningDays,
                SyncIntervalMinutes = syncIntervalMinutes
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(notificationSetting);
            }
            else
            {
                // Update existing setting
                notificationSetting.Value = updateDto.SendEmailNotifications.ToString();
                notificationSetting.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(pathSetting);
            }
            else
            {
                // Update existing setting
                pathSetting.Value = trimmedPath;
                pathSetting.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(designerWarningDaysSetting);
            }
            else
            {
                designerWarningDaysSetting.Value = updateDto.DesignerWarningDays.ToString();
                designerWarningDaysSetting.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(reviewerWarningDaysSetting);
            }
            else
            {
                reviewerWarningDaysSetting.Value = updateDto.ReviewerWarningDays.ToString();
                reviewerWarningDaysSetting.UpdatedAt = DateTime.UtcNow;
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

