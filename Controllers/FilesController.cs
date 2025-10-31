using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FilesController(ApplicationDbContext context)
    {
        _context = context;
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
        var file = await _context.Files.FindAsync(id);
        if (file == null)
        {
            return NotFound(new { message = "Không tìm thấy file để xóa" });
        }

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();

        return NoContent();
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
}
