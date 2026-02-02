using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.Data;
using quanlyfilesBE.Helpers;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FoldersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Folders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Folder>>> GetFolders()
    {
        return await _context.Folders
            .Include(f => f.ParentFolder)
            .ToListAsync();
    }

    // GET: api/Folders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Folder>> GetFolder(int id)
    {
        var folder = await _context.Folders
            .Include(f => f.ParentFolder)
            .FirstOrDefaultAsync(f => f.Id == id);
        
        if (folder == null)
        {
            return NotFound(new { message = "Không tìm thấy folder" });
        }
        
        return Ok(folder);
    }

    // GET: api/Folders/root
    [HttpGet("root")]
    public async Task<ActionResult<IEnumerable<Folder>>> GetRootFolders()
    {
        var rootFolders = await _context.Folders
            .Where(f => f.ParentFolderId == null)
            .ToListAsync();
        
        return Ok(rootFolders);
    }

    // POST: api/Folders
    [HttpPost]
    public async Task<ActionResult<Folder>> CreateFolder([FromBody] Folder folder)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        folder.CreatedDate = DateTimeHelper.NowVietnam();
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
    }

    // PUT: api/Folders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(int id, [FromBody] Folder folder)
    {
        if (id != folder.Id)
        {
            return BadRequest();
        }

        var existingFolder = await _context.Folders.FindAsync(id);
        if (existingFolder == null)
        {
            return NotFound(new { message = "Không tìm thấy folder để cập nhật" });
        }

        existingFolder.FolderName = folder.FolderName;
        existingFolder.FolderPath = folder.FolderPath;
        existingFolder.ParentFolderId = folder.ParentFolderId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FolderExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Folders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null)
        {
            return NotFound(new { message = "Không tìm thấy folder để xóa" });
        }

        // Check if folder has children
        var hasChildren = await _context.Folders.AnyAsync(f => f.ParentFolderId == id);
        if (hasChildren)
        {
            return BadRequest(new { message = "Không thể xóa folder có folder con" });
        }

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool FolderExists(int id)
    {
        return _context.Folders.Any(e => e.Id == id);
    }
}
