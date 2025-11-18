using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                return NotFound(new { message = "Không tìm thấy file để xóa" });
            }

            var filePath = file.FilePath;
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

            // Delete physical file from disk
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not delete physical file: {FilePath}", filePath);
                    // Continue with database deletion even if physical file deletion fails
                }
            }

            // Remove file record from database
            _context.Files.Remove(file);
            await _context.SaveChangesAsync();

            return NoContent();
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
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            // Generate unique filename to avoid conflicts
            var fileName = file.FileName;
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(storagePath, uniqueFileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Get file type from content type or extension
            var fileType = file.ContentType ?? fileExtension.TrimStart('.');

            // Get uploaded by from claims
            var uploadedBy = User.Identity?.Name ?? "Unknown";

            // Create file record
            var fileItem = new FileItem
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

            _context.Files.Add(fileItem);
            await _context.SaveChangesAsync();

            // Update MachineAssignment.FilePath - append new filePath with semicolon separator
            if (string.IsNullOrWhiteSpace(assignment.FilePath))
            {
                assignment.FilePath = filePath;
            }
            else
            {
                assignment.FilePath = $"{assignment.FilePath};{filePath}";
            }

            await _context.SaveChangesAsync();

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error uploading file: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error uploading file", message = ex.Message });
        }
    }
}
