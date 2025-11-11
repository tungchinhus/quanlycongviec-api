using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using System.IO;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ILogger<SettingsController>? _logger;

    public SettingsController(IOptions<FileStorageOptions> fileStorageOptions, ILogger<SettingsController>? logger = null)
    {
        _fileStorageOptions = fileStorageOptions.Value;
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
    public IActionResult GetFileStoragePath()
    {
        try
        {
            return Ok(new { path = _fileStorageOptions.Path });
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
    public IActionResult UpdateFileStoragePath([FromBody] UpdateFileStoragePathDto updateDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(updateDto.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            // Validate path
            var validationResult = ValidatePathInternal(updateDto.Path);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            // Return information that path needs to be updated in appsettings.json
            return BadRequest(new 
            { 
                error = "File storage path is configured in appsettings.json. Please update the 'FileStorage:Path' setting in appsettings.json and restart the application.",
                currentPath = _fileStorageOptions.Path,
                requestedPath = updateDto.Path.Trim()
            });
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
                return BadRequest(new ValidatePathResponseDto
                {
                    IsValid = false,
                    ErrorMessage = "Path is required"
                });
            }

            var validationResult = ValidatePathInternal(validateDto.Path);
            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ValidatePath: {Message}", ex.Message);
            return StatusCode(500, new ValidatePathResponseDto
            {
                IsValid = false,
                ErrorMessage = $"Error validating path: {ex.Message}"
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
}

