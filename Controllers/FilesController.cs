using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Claims;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;
using quanlyfilesBE.Helpers;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly IPowerAutomateService? _powerAutomateService;
    private readonly ILogger<FilesController>? _logger;

    public FilesController(
        ApplicationDbContext context, 
        IOptions<FileStorageOptions> fileStorageOptions,
        IPowerAutomateService? powerAutomateService = null,
        ILogger<FilesController>? logger = null)
    {
        _context = context;
        _fileStorageOptions = fileStorageOptions.Value;
        _powerAutomateService = powerAutomateService;
        _logger = logger;
    }

    // Helper methods to handle File_ID as comma-separated string
    private List<int> ParseFileIds(string? fileIds)
    {
        if (string.IsNullOrWhiteSpace(fileIds))
            return new List<int>();
        
        return fileIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    private string JoinFileIds(List<int> fileIds)
    {
        return string.Join(",", fileIds);
    }

    private bool ContainsFileId(string? fileIds, int fileId)
    {
        if (string.IsNullOrWhiteSpace(fileIds))
            return false;
        
        var ids = ParseFileIds(fileIds);
        return ids.Contains(fileId);
    }

    private string AddFileId(string? fileIds, int fileId)
    {
        var ids = ParseFileIds(fileIds);
        if (!ids.Contains(fileId))
        {
            ids.Add(fileId);
        }
        return JoinFileIds(ids);
    }

    private string RemoveFileId(string? fileIds, int fileId)
    {
        var ids = ParseFileIds(fileIds);
        ids.Remove(fileId);
        return ids.Any() ? JoinFileIds(ids) : null;
    }

    // GET: api/Files/search
    // Tra Cứu Files: tìm trực tiếp trong bảng FileIndex (SQL Server) – không gọi Python.
    [HttpGet("search")]
    public async Task<IActionResult> SearchFiles(
        [FromQuery] string folderPath,
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? ext = null,
        [FromQuery] int maxResults = 500)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(query))
            {
                return Ok(new { results = Array.Empty<object>(), usedIndex = true });
            }

            var folderPrefix = folderPath.Trim().Replace('/', '\\').TrimEnd('\\');
            var extList = string.IsNullOrWhiteSpace(ext)
                ? new List<string>()
                : ext.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().ToLowerInvariant()).ToList();
            var keywords = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 0)
                .ToList();

            const int scanCap = 4000;

            // Match trực tiếp path HOẶC path tương đương (vd: user nhập M:\P. Thiet Ke\LUU TRU, DB lưu UNC \\server\...\P. Thiet Ke\LUU TRU)
            string? relativePath = null;
            if (folderPrefix.Length >= 2 && folderPrefix[1] == ':' && (folderPrefix.Length == 2 || folderPrefix[2] == '\\') && folderPrefix.Length > 3)
                relativePath = folderPrefix.Substring(3).TrimStart('\\');

            // Include subfolders: when DB has UNC, match FolderPath that equals or is under the user's folder (e.g. ...\LUU TRU DU LIEU (THAO)\subfolder\...).
            var relativePathWithSep = relativePath != null && relativePath.Length > 0 ? "\\" + relativePath + "\\" : null;
            var queryable = _context.FileIndex
                .AsNoTracking()
                .Where(f =>
                    f.FolderPath == folderPrefix
                    || f.FolderPath.StartsWith(folderPrefix + "\\")
                    || (relativePath != null && relativePath.Length > 0 && (
                        f.FolderPath.EndsWith("\\" + relativePath)
                        || f.FolderPath == relativePath
                        || (relativePathWithSep != null && f.FolderPath.Contains(relativePathWithSep)))));

            // Filter by keywords in SQL so we don't pull 4000 non-matching rows (e.g. Thumbs.db) and miss actual matches.
            foreach (var kw in keywords)
                queryable = queryable.Where(f => f.NameNormalized != null && f.NameNormalized.Contains(kw));

            var candidates = await queryable
                .OrderByDescending(f => f.Mtime)
                .Take(scanCap)
                .Select(f => new { f.Name, f.FullPath, f.Ext, f.NameNormalized })
                .ToListAsync();

            var filtered = candidates.AsEnumerable();
            if (extList.Count > 0)
                filtered = filtered.Where(f => extList.Contains((f.Ext ?? string.Empty).ToLowerInvariant()));

            // Logic giống Python: _name_matches_keywords (bỏ dấu, mã có ranh giới, biến thể từ khóa)
            filtered = filtered.Where(f => FileSearchKeywordHelper.NameMatchesKeywords(f.Name, f.NameNormalized, keywords));

            var results = filtered
                .Take(Math.Max(1, maxResults))
                .Select(f => new { name = f.Name, path = f.FullPath, fullPath = f.FullPath })
                .ToList();

            var candidatesCount = candidates.Count;

            return Ok(new { results, usedIndex = true, candidatesCount });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FilesController.SearchFiles: {Message}", ex.Message);
            return StatusCode(500, new { results = Array.Empty<object>(), error = ex.Message });
        }
    }

    // GET: api/Files
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileItem>>> GetFiles()
    {
        return await _context.Files.ToListAsync();
    }

    // GET: api/Files/{id}/download - Phải đặt trước [HttpGet("{id}")] để tránh conflict routing
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                return NotFound(new { message = "Không tìm thấy file" });
            }

            var filePath = file.FilePath;
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "File không tồn tại trên server" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(file.FileType ?? Path.GetExtension(file.FileName));
            
            return File(fileBytes, contentType, file.FileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading file: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error downloading file", message = ex.Message });
        }
    }

    // GET: api/Files/5
    [HttpGet("{id}")]
    public async Task<ActionResult<FileItem>> GetFile(int id)
    {
        var file = await _context.Files.FindAsync(id);
        if (file == null)
        {
            return NotFound(new { message = "Không tìm thấy file" });
        }
        return Ok(file);
    }

    // POST: api/Files
    [HttpPost]
    public async Task<ActionResult<FileItem>> CreateFile([FromBody] CreateFileItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newFile = new FileItem
        {
            AssignmentID = dto.AssignmentID,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            FileType = dto.FileType,
            FileSize = dto.FileSize,
            UploadDate = DateTimeHelper.NowVietnam(),
            UploadedBy = dto.UploadedBy,
            Description = dto.Description
        };

        _context.Files.Add(newFile);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFile), new { id = newFile.Id }, newFile);
    }

    // PUT: api/Files/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFile(int id, [FromBody] UpdateFileItemDto dto)
    {
        var existingFile = await _context.Files.FindAsync(id);
        if (existingFile == null)
        {
            return NotFound(new { message = "Không tìm thấy file để cập nhật" });
        }

        if (!string.IsNullOrEmpty(dto.FileName))
            existingFile.FileName = dto.FileName;

        if (!string.IsNullOrEmpty(dto.FileType))
            existingFile.FileType = dto.FileType;

        if (!string.IsNullOrEmpty(dto.Description))
            existingFile.Description = dto.Description;

        if (!string.IsNullOrEmpty(dto.UploadedBy))
            existingFile.UploadedBy = dto.UploadedBy;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Files/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        // Check if file exists first
        var file = await _context.Files.FindAsync(id);
        if (file == null)
        {
            return NotFound(new { message = "Không tìm thấy file để xóa" });
        }

        // Use execution strategy to support retries with transaction
        string? filePath = file.FilePath;
        var assignmentId = file.AssignmentID;
        var strategy = _context.Database.CreateExecutionStrategy();
        
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Re-fetch file within transaction to ensure we have the latest data
                    var fileInTransaction = await _context.Files.FindAsync(id);
                    if (fileInTransaction == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    // If file has AssignmentID, update File_ID if this was the primary file
                    if (assignmentId.HasValue)
                    {
                        var assignment = await _context.MachineAssignments.FindAsync(assignmentId.Value);
                        if (assignment != null && assignment.File_ID == fileInTransaction.Id)
                        {
                            // If this was the primary file (File_ID), find the next file for this assignment
                            var nextFile = await _context.Files
                                .Where(f => f.AssignmentID == assignmentId.Value && f.Id != fileInTransaction.Id)
                                .OrderBy(f => f.UploadDate)
                                .FirstOrDefaultAsync();
                            
                            if (nextFile != null)
                            {
                                assignment.File_ID = nextFile.Id;
                                _logger?.LogInformation("Updated File_ID to {FileId} after deleting primary file {DeletedFileId}", nextFile.Id, fileInTransaction.Id);
                            }
                            else
                            {
                                assignment.File_ID = null;
                                _logger?.LogInformation("Cleared File_ID after deleting last file for AssignmentID {AssignmentID}", assignmentId.Value);
                            }
                        }

                        // Update File_ID for WorkItems that reference this file
                        // File_ID is now a comma-separated string, so we need to check if it contains the file ID
                        var allWorkItems = await _context.WorkItems
                            .Where(wi => wi.AssignmentID == assignmentId.Value)
                            .ToListAsync();
                        
                        var workItemsWithThisFile = allWorkItems
                            .Where(wi => ContainsFileId(wi.File_ID, fileInTransaction.Id))
                            .ToList();
                        
                        if (workItemsWithThisFile.Any())
                        {
                            // Remove the deleted file ID from File_ID string
                            foreach (var workItem in workItemsWithThisFile)
                            {
                                workItem.File_ID = RemoveFileId(workItem.File_ID, fileInTransaction.Id);
                            }
                            _logger?.LogInformation("Removed file ID {FileId} from File_ID for {Count} WorkItems", 
                                fileInTransaction.Id, workItemsWithThisFile.Count);
                        }
                    }
                    
                    // Note: We no longer update FilePath in MachineAssignment since files are tracked in Files table via AssignmentID

                    // Remove file record from database FIRST
                    _context.Files.Remove(fileInTransaction);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(dbEx, "Database error deleting file: {Message}", dbEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger?.LogError(ex, "Error deleting file: {Message}", ex.Message);
                    throw;
                }
            });

            // Only delete physical file AFTER successful database deletion
            if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    _logger?.LogInformation("Deleted physical file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not delete physical file after database deletion: {FilePath}", filePath);
                    // Database record is already deleted, so we continue
                }
            }

            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error deleting file: {Message}", dbEx.Message);
            return StatusCode(500, new { error = "Error deleting file from database", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting file: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting file", message = ex.Message });
        }
    }

    // GET: api/Files/byType/{fileType}
    [HttpGet("byType/{fileType}")]
    public async Task<ActionResult<IEnumerable<FileItem>>> GetFilesByType(string fileType)
    {
        var files = await _context.Files
            .Where(f => f.FileType.Contains(fileType))
            .ToListAsync();
        
        return Ok(files);
    }

    // GET: api/Files/byAssignment/{assignmentId}
    [HttpGet("byAssignment/{assignmentId}")]
    public async Task<ActionResult<IEnumerable<FileItem>>> GetFilesByAssignment(int assignmentId)
    {
        var files = await _context.Files
            .Where(f => f.AssignmentID == assignmentId)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
        
        return Ok(files);
    }

    private string GetContentType(string fileType)
    {
        var extension = Path.GetExtension(fileType).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Sanitize username để sử dụng làm tên folder (loại bỏ ký tự không hợp lệ)
    /// </summary>
    private string SanitizeFolderName(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Unknown";
        
        // Loại bỏ các ký tự không hợp lệ cho tên folder: / \ : * ? " < > |
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToArray();
        var sanitized = string.Join("_", username.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Loại bỏ khoảng trắng ở đầu và cuối, thay thế khoảng trắng bằng underscore
        sanitized = sanitized.Trim().Replace(" ", "_");
        
        // Đảm bảo không rỗng
        if (string.IsNullOrWhiteSpace(sanitized))
            return "Unknown";
        
        return sanitized;
    }

    /// <summary>
    /// Tạo tên file duy nhất bằng cách thêm số thứ tự nếu file đã tồn tại
    /// Ví dụ: file.pdf -> file (1).pdf -> file (2).pdf
    /// </summary>
    private string GetUniqueFileName(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        
        // Nếu file chưa tồn tại, trả về tên gốc
        if (!System.IO.File.Exists(filePath))
        {
            return fileName;
        }
        
        // Tách tên file và extension
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        
        // Thử thêm số thứ tự từ 1 đến 9999
        for (int i = 1; i <= 9999; i++)
        {
            var newFileName = $"{fileNameWithoutExtension} ({i}){extension}";
            var newFilePath = Path.Combine(directory, newFileName);
            
            if (!System.IO.File.Exists(newFilePath))
            {
                _logger?.LogInformation("File name conflict resolved: {OriginalFileName} -> {NewFileName}", fileName, newFileName);
                return newFileName;
            }
        }
        
        // Nếu không tìm được tên duy nhất trong 9999 lần thử, thêm timestamp
        var timestamp = DateTimeHelper.NowVietnam().ToString("yyyyMMdd_HHmmss");
        var timestampFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
        _logger?.LogWarning("Using timestamp for unique file name: {OriginalFileName} -> {TimestampFileName}", fileName, timestampFileName);
        return timestampFileName;
    }

    /// <summary>
    /// Lấy username của user đang đăng nhập từ claims hoặc database
    /// </summary>
    private async Task<string> GetCurrentUsernameAsync()
    {
        // Thử lấy từ User.Identity.Name trước (từ ClaimTypes.Name)
        var username = User.Identity?.Name;
        
        if (!string.IsNullOrWhiteSpace(username))
        {
            _logger?.LogInformation("Got username from User.Identity.Name: {Username}", username);
            return username;
        }
        
        // Thử lấy từ ClaimTypes.Name trực tiếp
        var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(nameClaim))
        {
            _logger?.LogInformation("Got username from ClaimTypes.Name: {Username}", nameClaim);
            return nameClaim;
        }
        
        // Thử lấy từ JwtRegisteredClaimNames.Name
        var jwtNameClaim = User.FindFirst("name")?.Value;
        if (!string.IsNullOrWhiteSpace(jwtNameClaim))
        {
            _logger?.LogInformation("Got username from 'name' claim: {Username}", jwtNameClaim);
            return jwtNameClaim;
        }
        
        // Nếu không có, thử lấy từ database thông qua FirebaseUID claim
        var firebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        
        if (!string.IsNullOrEmpty(firebaseUID))
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
            
            if (user == null)
            {
                // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
                                ?? User.FindFirst("email")?.Value;
                
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == emailClaim);
                }
            }
            
            if (user != null)
            {
                // Ưu tiên UserName, sau đó FullName, cuối cùng là UserId
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    _logger?.LogInformation("Got username from database UserName: {Username}", user.UserName);
                    return user.UserName;
                }
                if (!string.IsNullOrWhiteSpace(user.FullName))
                {
                    _logger?.LogInformation("Got username from database FullName: {Username}", user.FullName);
                    return user.FullName;
                }
            }
        }
        
        // Log warning nếu không lấy được username
        _logger?.LogWarning("Could not get username from claims or database. FirebaseUID: {FirebaseUID}, IdentityName: {IdentityName}", 
            firebaseUID, User.Identity?.Name);
        
            return "Unknown";
    }

    private async Task<string> GetFileStoragePathAsync()
    {
        // Check database first, then fall back to appsettings.json
        var pathSetting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == "file-storage-path");
        
        var fileStoragePath = pathSetting != null 
            ? pathSetting.Value 
            : _fileStorageOptions.Path;

        // If still empty, use default
        if (string.IsNullOrWhiteSpace(fileStoragePath))
        {
            fileStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        }

        return fileStoragePath;
    }

    private async Task HandleFileUploadNotificationAsync(FileItem fileItem, MachineAssignment assignment, string uploadedBy)
    {
        // Get notification preference from settings
        var notificationSetting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
        
        var sendEmailNotifications = true; // Default to email
        if (notificationSetting != null)
        {
            bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
        }

        // Get current user info for notifications
        var firebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);

        if (sendEmailNotifications)
        {
            // Send email notification
            if (_powerAutomateService != null && currentUser != null && !string.IsNullOrEmpty(currentUser.Email))
            {
                var subject = $"File đã được upload: {fileItem.FileName}";
                var body = $@"
                    <h2>File đã được upload thành công</h2>
                    <p><strong>File:</strong> {fileItem.FileName}</p>
                    <p><strong>Kích thước:</strong> {FormatFileSize(fileItem.FileSize)}</p>
                    <p><strong>Người upload:</strong> {uploadedBy}</p>
                    <p><strong>Máy:</strong> {assignment.MachineName}</p>
                    <p><strong>Ngày upload:</strong> {fileItem.UploadDate:dd/MM/yyyy HH:mm}</p>
                    {(string.IsNullOrEmpty(fileItem.Description) ? "" : $"<p><strong>Mô tả:</strong> {fileItem.Description}</p>")}
                ";

                // Create a minimal ApprovalWorkflowDto for email
                var workflowDto = new ApprovalWorkflowDto
                {
                    WorkflowID = 0,
                    RequestTitle = $"File upload: {fileItem.FileName}",
                    RequesterName = uploadedBy,
                    RequesterEmail = currentUser.Email
                };

                await _powerAutomateService.SendNotificationEmailAsync(
                    currentUser.Email,
                    subject,
                    body,
                    workflowDto);
            }
        }
        else
        {
            // Create notification badge
            if (!string.IsNullOrEmpty(firebaseUID))
            {
                var notification = new Notification
                {
                    UserId = firebaseUID,
                    Title = "File đã được upload",
                    Message = $"File '{fileItem.FileName}' đã được upload thành công cho máy {assignment.MachineName}",
                    Type = "success",
                    IsRead = false,
                    CreatedAt = DateTimeHelper.NowVietnam(),
                    RelatedEntityType = "File",
                    RelatedEntityId = fileItem.Id
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    // POST: api/Files/upload
    [HttpPost("upload")]
    public async Task<ActionResult<FileItem>> UploadFile(IFormFile file, [FromForm] string? description = null, [FromForm] int assignmentId = 0)
    {
        try
        {
            _logger?.LogInformation("Upload file request received. FileName: {FileName}, FileSize: {FileSize}, AssignmentId: {AssignmentId}, Description: {Description}",
                file?.FileName, file?.Length, assignmentId, description);

            if (file == null || file.Length == 0)
            {
                _logger?.LogWarning("Upload file request rejected: No file uploaded");
                return BadRequest(new { error = "No file uploaded" });
            }

            if (assignmentId <= 0)
            {
                _logger?.LogWarning("Upload file request rejected: Invalid assignmentId {AssignmentId}", assignmentId);
                return BadRequest(new { error = "assignmentId is required" });
            }

            // Validate assignment exists
            var assignment = await _context.MachineAssignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                _logger?.LogWarning("Upload file request rejected: Assignment {AssignmentId} not found", assignmentId);
                return NotFound(new { error = "Assignment not found" });
            }
            
            _logger?.LogInformation("Assignment {AssignmentId} found: {MachineName}", assignmentId, assignment.MachineName);

            // Validate file size (e.g., max 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds maximum allowed size (50MB)" });
            }

            // Get storage path (from database or appsettings.json)
            var baseStoragePath = await GetFileStoragePathAsync();

            // Lấy username của user đang đăng nhập và tạo folder theo username
            var currentUsername = await GetCurrentUsernameAsync();
            var sanitizedUsername = SanitizeFolderName(currentUsername);
            
            _logger?.LogInformation("Current username: {CurrentUsername}, Sanitized: {SanitizedUsername}", 
                currentUsername, sanitizedUsername);
            
            // Tạo đường dẫn lưu trữ theo username: storagePath/username/
            var userStoragePath = Path.Combine(baseStoragePath, sanitizedUsername);
            
            _logger?.LogInformation("Base storage path: {BaseStoragePath}, User storage path: {UserStoragePath}", 
                baseStoragePath, userStoragePath);

            // Create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(baseStoragePath))
                {
                    Directory.CreateDirectory(baseStoragePath);
                    _logger?.LogInformation("Created base storage directory: {StoragePath}", baseStoragePath);
                }
                
                if (!Directory.Exists(userStoragePath))
                {
                    Directory.CreateDirectory(userStoragePath);
                    _logger?.LogInformation("Created user storage directory: {UserStoragePath} for user: {Username}", userStoragePath, currentUsername);
                }
            }
            catch (Exception dirEx)
            {
                _logger?.LogError(dirEx, "Error creating storage directory: {UserStoragePath}", userStoragePath);
                return StatusCode(500, new { error = "Error creating storage directory", message = dirEx.Message, path = userStoragePath });
            }

            // Lấy tên file gốc và tạo tên file duy nhất nếu trùng
            var originalFileName = Path.GetFileName(file.FileName);
            var uniqueFileName = GetUniqueFileName(userStoragePath, originalFileName);
            var fileExtension = Path.GetExtension(uniqueFileName);
            
            // Lưu file vào folder của user: storagePath/username/filename
            var filePath = Path.Combine(userStoragePath, uniqueFileName);
            
            // Log nếu tên file đã được thay đổi
            if (originalFileName != uniqueFileName)
            {
                _logger?.LogInformation("File name changed due to conflict: {OriginalFileName} -> {UniqueFileName}", originalFileName, uniqueFileName);
            }
            
            _logger?.LogInformation("File will be saved to: {FilePath}", filePath);

            // Save file to disk
            try
            {
                _logger?.LogInformation("Starting to save file to disk: {FilePath}, Size: {FileSize} bytes", filePath, file.Length);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger?.LogInformation("File saved successfully to disk: {FilePath}", filePath);
                
                // Verify file was saved
                if (System.IO.File.Exists(filePath))
                {
                    var savedFileInfo = new FileInfo(filePath);
                    _logger?.LogInformation("File verified on disk: {FilePath}, Size: {FileSize} bytes", filePath, savedFileInfo.Length);
                }
                else
                {
                    _logger?.LogError("File was not found on disk after save: {FilePath}", filePath);
                    return StatusCode(500, new { error = "File was not saved correctly", message = "File not found after save operation", path = filePath });
                }
            }
            catch (Exception saveEx)
            {
                _logger?.LogError(saveEx, "Error saving file to disk: {FilePath}, Error: {ErrorMessage}", filePath, saveEx.Message);
                return StatusCode(500, new { error = "Error saving file to disk", message = saveEx.Message, path = filePath });
            }

            // Get file type from content type or extension
            var fileType = file.ContentType ?? fileExtension.TrimStart('.');

            // Get uploaded by - sử dụng currentUsername đã lấy ở trên
            var uploadedBy = currentUsername;

            // Use execution strategy to support retries with transaction
            FileItem? fileItem = null;
            var strategy = _context.Database.CreateExecutionStrategy();
            
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    // Use Serializable isolation level to prevent race condition when uploading multiple files in parallel
                    await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    try
                    {
                        // Create file record - sử dụng uniqueFileName thay vì fileName gốc
                        fileItem = new FileItem
                        {
                            AssignmentID = assignmentId,
                            FileName = uniqueFileName, // Lưu tên file đã được đổi (nếu có)
                            FilePath = filePath,
                            FileType = fileType,
                            FileSize = file.Length,
                            UploadDate = DateTimeHelper.NowVietnam(),
                            UploadedBy = uploadedBy,
                            Description = description
                        };

                        // Save file first to get its Id
                        _context.Files.Add(fileItem);
                        await _context.SaveChangesAsync();

                        // Update MachineAssignment.File_ID with the uploaded file's Id
                        // If this is the first file or File_ID is null, set it to this file's Id
                        // Note: We no longer update FilePath in MachineAssignment since files are tracked in Files table via AssignmentID
                        if (!assignment.File_ID.HasValue)
                        {
                            assignment.File_ID = fileItem.Id;
                            _logger?.LogInformation("Setting File_ID to {FileId} for AssignmentID {AssignmentID}", fileItem.Id, assignmentId);
                        }

                        // Update File_ID only for WorkItems that belong to the current user
                        // Get current user info from claims
                        var firebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value;
                        var currentUserName = User.Identity?.Name;
                        
                        // Try to get user from database to match with WorkItem.PersonName
                        User? currentUser = null;
                        if (!string.IsNullOrEmpty(firebaseUID))
                        {
                            currentUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
                            
                            if (currentUser == null)
                            {
                                // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
                                                ?? User.FindFirst("email")?.Value;
                                
                                if (!string.IsNullOrEmpty(emailClaim))
                                {
                                    currentUser = await _context.Users
                                        .FirstOrDefaultAsync(u => u.Email == emailClaim);
                                }
                            }
                        }
                        
                        // Get all possible identifiers for current user
                        var userIdentifiers = new List<string>();
                        if (currentUser != null)
                        {
                            userIdentifiers.Add(currentUser.UserId.ToString());
                            if (!string.IsNullOrEmpty(currentUser.UserName))
                                userIdentifiers.Add(currentUser.UserName);
                            if (!string.IsNullOrEmpty(currentUser.FullName))
                                userIdentifiers.Add(currentUser.FullName);
                        }
                        if (!string.IsNullOrEmpty(currentUserName))
                            userIdentifiers.Add(currentUserName);
                        if (!string.IsNullOrEmpty(firebaseUID))
                            userIdentifiers.Add(firebaseUID);
                        
                        // Find WorkItems that belong to current user
                        var workItems = await _context.WorkItems
                            .Where(wi => wi.AssignmentID == assignmentId)
                            .ToListAsync();
                        
                        var userWorkItems = workItems
                            .Where(wi => !string.IsNullOrEmpty(wi.PersonName) && 
                                         userIdentifiers.Any(id => 
                                             wi.PersonName.Equals(id, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                        
                        if (userWorkItems.Any())
                        {
                            // Reload work items from database with row lock to prevent race condition
                            // This ensures that when multiple files are uploaded in parallel,
                            // each upload will read the latest File_ID value and update it correctly
                            var workItemIds = userWorkItems.Select(wi => wi.WorkItemID).ToList();
                            
                            // Reload and update work items one by one with proper locking
                            // This ensures that when multiple files are uploaded in parallel,
                            // each upload will read the latest File_ID value and update it correctly
                            foreach (var workItemId in workItemIds)
                            {
                                // Reload work item from database within transaction to get latest File_ID
                                // Using FirstOrDefaultAsync ensures we get fresh data from database
                                // With Serializable isolation level, this will lock the row until transaction completes
                                var workItem = await _context.WorkItems
                                    .FirstOrDefaultAsync(wi => wi.WorkItemID == workItemId);
                                
                                if (workItem != null)
                                {
                                    // Add file ID to File_ID string if not already present
                                    var currentFileId = workItem.File_ID;
                                    if (!ContainsFileId(currentFileId, fileItem.Id))
                                    {
                                        workItem.File_ID = AddFileId(currentFileId, fileItem.Id);
                                        _logger?.LogInformation("Adding file ID {FileId} to File_ID for WorkItem {WorkItemID}. Old File_ID: {OldFileId}, New File_ID: {NewFileId}", 
                                            fileItem.Id, workItem.WorkItemID, currentFileId ?? "null", workItem.File_ID);
                                    }
                                    else
                                    {
                                        _logger?.LogInformation("File ID {FileId} already exists in File_ID for WorkItem {WorkItemID}. Current File_ID: {CurrentFileId}", 
                                            fileItem.Id, workItem.WorkItemID, currentFileId ?? "null");
                                    }
                                }
                            }
                            
                            _logger?.LogInformation("Added file ID {FileId} to File_ID for {Count} WorkItems of current user (AssignmentID {AssignmentID})", 
                                fileItem.Id, workItemIds.Count, assignmentId);
                        }
                        else
                        {
                            _logger?.LogInformation("No matching WorkItems found for current user (AssignmentID {AssignmentID}, UserIdentifiers: {UserIdentifiers})", 
                                assignmentId, string.Join(", ", userIdentifiers));
                        }
                        
                        // Save all changes (assignment and work items)
                        await _context.SaveChangesAsync();
                        
                        // Commit transaction if everything succeeds
                        await transaction.CommitAsync();
                        _logger?.LogInformation("File uploaded and saved successfully: {FilePath}", filePath);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        await transaction.RollbackAsync();
                        _logger?.LogError(dbEx, "Database error saving file: {Message}", dbEx.Message);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger?.LogError(ex, "Unexpected error uploading file: {Message}", ex.Message);
                        throw;
                    }
                });

                // Safety check: fileItem must be assigned if transaction succeeded
                if (fileItem == null)
                {
                    _logger?.LogError("fileItem is null after successful execution strategy run.");
                    return StatusCode(500, new { error = "Error uploading file", message = "fileItem not created" });
                }

                // Handle notifications based on system settings
                try
                {
                    // Không tạo notification khi upload file
                    // await HandleFileUploadNotificationAsync(fileItem, assignment, currentUsername);
                }
                catch (Exception notifEx)
                {
                    // Log but don't fail the upload if notification fails
                    _logger?.LogWarning(notifEx, "Failed to send notification for file upload: {Message}", notifEx.Message);
                }

                // Create response after successful commit
                var response = new
                {
                    id = fileItem.Id,
                    fileName = fileItem.FileName,
                    filePath = filePath,
                    fileType = fileItem.FileType,
                    fileSize = fileItem.FileSize,
                    uploadDate = fileItem.UploadDate,
                    uploadedBy = fileItem.UploadedBy,
                    description = fileItem.Description,
                    assignmentId = assignmentId
                };

                return Ok(response);
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error saving file: {Message}", dbEx.Message);
                
                // Cleanup file on disk if database save failed
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger?.LogInformation("Cleaned up file after database error: {FilePath}", filePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger?.LogWarning(cleanupEx, "Could not cleanup file after database error: {FilePath}", filePath);
                }
                
                return StatusCode(500, new { error = "Error saving file to database", message = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error uploading file: {Message}", ex.Message);
                
                // Cleanup file on disk if any error occurs
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger?.LogInformation("Cleaned up file after error: {FilePath}", filePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger?.LogWarning(cleanupEx, "Could not cleanup file after error: {FilePath}", filePath);
                }
                
                return StatusCode(500, new { error = "Error uploading file", message = ex.Message });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error uploading file: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error uploading file", message = ex.Message });
        }
    }
}
