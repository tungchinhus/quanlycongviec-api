using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ILogger<FilesController>? _logger;

    public FilesController(
        ApplicationDbContext context, 
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<FilesController>? logger = null)
    {
        _context = context;
        _fileStorageOptions = fileStorageOptions.Value;
        _logger = logger;
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
            UploadDate = DateTime.Now,
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

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Files/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        // Use transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        string? filePath = null;
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = "Không tìm thấy file để xóa" });
            }

            filePath = file.FilePath;
            var assignmentId = file.AssignmentID;

            // If file has AssignmentID, update that specific assignment
            if (assignmentId.HasValue)
            {
                var assignment = await _context.MachineAssignments.FindAsync(assignmentId.Value);
                if (assignment != null && !string.IsNullOrWhiteSpace(assignment.FilePath))
                {
                    var filePaths = assignment.FilePath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Where(fp => fp.Trim() != filePath.Trim())
                        .ToList();

                    if (filePaths.Any())
                    {
                        assignment.FilePath = string.Join(";", filePaths);
                    }
                    else
                    {
                        assignment.FilePath = null;
                    }
                }
            }
            else
            {
                // Fallback: Find all MachineAssignments that contain this filePath (for backward compatibility)
                var assignments = await _context.MachineAssignments
                    .Where(a => a.FilePath != null && a.FilePath.Contains(filePath))
                    .ToListAsync();

                // Remove filePath from each assignment's FilePath
                foreach (var assignment in assignments)
                {
                    if (!string.IsNullOrWhiteSpace(assignment.FilePath))
                    {
                        var filePaths = assignment.FilePath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Where(fp => fp.Trim() != filePath.Trim())
                            .ToList();

                        if (filePaths.Any())
                        {
                            assignment.FilePath = string.Join(";", filePaths);
                        }
                        else
                        {
                            assignment.FilePath = null;
                        }
                    }
                }
            }

            // Remove file record from database FIRST
            _context.Files.Remove(file);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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
            await transaction.RollbackAsync();
            _logger?.LogError(dbEx, "Database error deleting file: {Message}", dbEx.Message);
            return StatusCode(500, new { error = "Error deleting file from database", message = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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

    // POST: api/Files/upload
    [HttpPost("upload")]
    public async Task<ActionResult<FileItem>> UploadFile(IFormFile file, [FromForm] string? description = null, [FromForm] int assignmentId = 0)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (assignmentId <= 0)
            {
                return BadRequest(new { error = "assignmentId is required" });
            }

            // Validate assignment exists
            var assignment = await _context.MachineAssignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            // Validate file size (e.g., max 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds maximum allowed size (50MB)" });
            }

            // Get storage path
            var storagePath = _fileStorageOptions.Path;
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            }

            // Create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(storagePath))
                {
                    Directory.CreateDirectory(storagePath);
                    _logger?.LogInformation("Created storage directory: {StoragePath}", storagePath);
                }
            }
            catch (Exception dirEx)
            {
                _logger?.LogError(dirEx, "Error creating storage directory: {StoragePath}", storagePath);
                return StatusCode(500, new { error = "Error creating storage directory", message = dirEx.Message, path = storagePath });
            }

            // Generate unique filename to avoid conflicts
            var fileName = file.FileName;
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(storagePath, uniqueFileName);

            // Save file to disk
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger?.LogInformation("File saved successfully: {FilePath}", filePath);
            }
            catch (Exception saveEx)
            {
                _logger?.LogError(saveEx, "Error saving file to disk: {FilePath}", filePath);
                return StatusCode(500, new { error = "Error saving file to disk", message = saveEx.Message, path = filePath });
            }

            // Get file type from content type or extension
            var fileType = file.ContentType ?? fileExtension.TrimStart('.');

            // Get uploaded by from claims
            var uploadedBy = User.Identity?.Name ?? "Unknown";

            // Use transaction to ensure atomicity - if database save fails, rollback and cleanup file
            FileItem fileItem;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create file record
                fileItem = new FileItem
                {
                    AssignmentID = assignmentId,
                    FileName = fileName,
                    FilePath = filePath,
                    FileType = fileType,
                    FileSize = file.Length,
                    UploadDate = DateTime.Now,
                    UploadedBy = uploadedBy,
                    Description = description
                };

                // Update MachineAssignment.FilePath - append new filePath with semicolon separator
                if (string.IsNullOrWhiteSpace(assignment.FilePath))
                {
                    assignment.FilePath = filePath;
                }
                else
                {
                    // Check if filePath length would exceed max length (4000 chars)
                    var newFilePath = $"{assignment.FilePath};{filePath}";
                    if (newFilePath.Length > 4000)
                    {
                        _logger?.LogWarning("FilePath would exceed max length. Truncating or skipping assignment update.");
                        // Don't update assignment, just save file
                    }
                    else
                    {
                        assignment.FilePath = newFilePath;
                    }
                }

                // Save both changes in a single transaction
                _context.Files.Add(fileItem);
                await _context.SaveChangesAsync();
                
                // Commit transaction if everything succeeds
                await transaction.CommitAsync();
                _logger?.LogInformation("File uploaded and saved successfully: {FilePath}", filePath);

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
                await transaction.RollbackAsync();
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
                await transaction.RollbackAsync();
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
