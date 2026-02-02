using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.Helpers;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSignatureController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<UserSignatureController>? _logger;

    public UserSignatureController(
        ApplicationDbContext context,
        IFirebaseService firebaseService,
        ILogger<UserSignatureController>? logger = null)
    {
        _context = context;
        _firebaseService = firebaseService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy đường dẫn lưu chữ ký từ settings
    /// </summary>
    private async Task<string> GetSignatureStoragePathAsync()
    {
        try
        {
            // Check database first
            var pathSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "signature-storage-path");
            
            var signatureStoragePath = pathSetting != null 
                ? pathSetting.Value 
                : Path.Combine(Directory.GetCurrentDirectory(), "signatures"); // Default fallback

            if (string.IsNullOrWhiteSpace(signatureStoragePath))
            {
                signatureStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "signatures");
            }

            return signatureStoragePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting signature storage path, using default");
            return Path.Combine(Directory.GetCurrentDirectory(), "signatures");
        }
    }

    /// <summary>
    /// Lấy user hiện tại từ Firebase token
    /// </summary>
    private async Task<Models.User?> GetCurrentUserAsync()
    {
        try
        {
            // Lấy FirebaseUID từ claims
            var firebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("firebase_uid")?.Value;

            if (string.IsNullOrEmpty(firebaseUID))
            {
                _logger?.LogWarning("FirebaseUID not found in claims");
                return null;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);

            if (user == null)
            {
                _logger?.LogWarning("User not found for FirebaseUID: {FirebaseUID}", firebaseUID);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting current user");
            return null;
        }
    }

    /// <summary>
    /// Upload chữ ký điện tử của user hiện tại
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadSignature(IFormFile file)
    {
        try
        {
            // Lấy user hiện tại
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found or not authenticated" });
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            // Validate file type - chỉ cho phép image
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = "Invalid file type. Only image files are allowed." });
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds maximum allowed size (5MB)" });
            }

            // Lấy đường dẫn lưu chữ ký từ settings
            var signaturesDirectory = await GetSignatureStoragePathAsync();
            if (!Directory.Exists(signaturesDirectory))
            {
                Directory.CreateDirectory(signaturesDirectory);
                _logger?.LogInformation("Created signatures directory: {Directory}", signaturesDirectory);
            }

            // Tạo tên file duy nhất theo UserId
            var uniqueFileName = $"signature_{currentUser.UserId}_{DateTimeHelper.NowVietnam():yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(signaturesDirectory, uniqueFileName);

            // Xóa file chữ ký cũ nếu có
            if (!string.IsNullOrEmpty(currentUser.SignaturePath) && System.IO.File.Exists(currentUser.SignaturePath))
            {
                try
                {
                    System.IO.File.Delete(currentUser.SignaturePath);
                    _logger?.LogInformation("Deleted old signature file: {OldPath}", currentUser.SignaturePath);
                }
                catch (Exception deleteEx)
                {
                    _logger?.LogWarning(deleteEx, "Could not delete old signature file: {OldPath}", currentUser.SignaturePath);
                }
            }

            // Lưu file mới
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger?.LogInformation("Signature file saved: {FilePath} for user {UserId}", filePath, currentUser.UserId);

            // Cập nhật path trong database
            currentUser.SignaturePath = filePath;
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Signature path updated in database for user {UserId}", currentUser.UserId);

            return Ok(new
            {
                message = "Signature uploaded successfully",
                userId = currentUser.UserId,
                signaturePath = filePath
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error uploading signature");
            return StatusCode(500, new { error = "Error uploading signature", message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chữ ký điện tử của user hiện tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSignature()
    {
        try
        {
            // Lấy user hiện tại
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found or not authenticated" });
            }

            // Kiểm tra xem user có chữ ký không
            if (string.IsNullOrEmpty(currentUser.SignaturePath))
            {
                return NotFound(new { error = "Signature not found" });
            }

            // Kiểm tra file có tồn tại không
            if (!System.IO.File.Exists(currentUser.SignaturePath))
            {
                _logger?.LogWarning("Signature file not found on disk: {Path}", currentUser.SignaturePath);
                // Xóa path trong database nếu file không tồn tại
                currentUser.SignaturePath = null;
                await _context.SaveChangesAsync();
                return NotFound(new { error = "Signature file not found" });
            }

            // Đọc file và trả về
            var fileBytes = await System.IO.File.ReadAllBytesAsync(currentUser.SignaturePath);
            var contentType = GetContentType(Path.GetExtension(currentUser.SignaturePath));

            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting signature");
            return StatusCode(500, new { error = "Error getting signature", message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa chữ ký điện tử của user hiện tại
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteSignature()
    {
        try
        {
            // Lấy user hiện tại
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found or not authenticated" });
            }

            // Kiểm tra xem user có chữ ký không
            if (string.IsNullOrEmpty(currentUser.SignaturePath))
            {
                return NotFound(new { error = "Signature not found" });
            }

            // Xóa file
            if (System.IO.File.Exists(currentUser.SignaturePath))
            {
                try
                {
                    System.IO.File.Delete(currentUser.SignaturePath);
                    _logger?.LogInformation("Deleted signature file: {Path}", currentUser.SignaturePath);
                }
                catch (Exception deleteEx)
                {
                    _logger?.LogWarning(deleteEx, "Could not delete signature file: {Path}", currentUser.SignaturePath);
                }
            }

            // Xóa path trong database
            currentUser.SignaturePath = null;
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Signature deleted for user {UserId}", currentUser.UserId);

            return Ok(new { message = "Signature deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting signature");
            return StatusCode(500, new { error = "Error deleting signature", message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chữ ký (không trả về file, chỉ metadata)
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetSignatureInfo()
    {
        try
        {
            // Lấy user hiện tại
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found or not authenticated" });
            }

            if (string.IsNullOrEmpty(currentUser.SignaturePath))
            {
                return Ok(new { hasSignature = false });
            }

            var fileExists = System.IO.File.Exists(currentUser.SignaturePath);
            if (!fileExists)
            {
                // Xóa path trong database nếu file không tồn tại
                currentUser.SignaturePath = null;
                await _context.SaveChangesAsync();
                return Ok(new { hasSignature = false });
            }

            var fileInfo = new FileInfo(currentUser.SignaturePath);
            return Ok(new
            {
                hasSignature = true,
                fileSize = fileInfo.Length,
                lastModified = fileInfo.LastWriteTimeUtc
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting signature info");
            return StatusCode(500, new { error = "Error getting signature info", message = ex.Message });
        }
    }

    /// <summary>
    /// Xác định content type từ file extension
    /// </summary>
    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
