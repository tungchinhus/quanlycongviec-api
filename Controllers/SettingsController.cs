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

            var settings = new SystemSettingsDto
            {
                FileStoragePath = fileStoragePath,
                SendEmailNotifications = sendEmailNotifications
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
}

