using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/page-permissions")]
[Authorize]
public class PagePermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PagePermissionsController>? _logger;

    public PagePermissionsController(
        ApplicationDbContext context,
        ILogger<PagePermissionsController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }

    // GET: api/page-permissions
    [HttpGet]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<ActionResult<IEnumerable<PagePermissionDto>>> GetPagePermissions()
    {
        try
        {
            _logger?.LogInformation("GetPagePermissions called");
            var pages = await _context.PagePermissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.PageName)
                .ToListAsync();
            
            _logger?.LogInformation("GetPagePermissions: Found {Count} pages", pages.Count);

            var dtos = pages.Select(p => new PagePermissionDto
            {
                Id = p.Id,
                PageRoute = p.PageRoute,
                PageName = p.PageName,
                Description = p.Description,
                IsActive = p.IsActive
            }).ToList();

            _logger?.LogInformation("GetPagePermissions: Returning {Count} page permissions", dtos.Count);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetPagePermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving page permissions", message = ex.Message });
        }
    }

    // GET: api/page-permissions/user/{userId}
    [HttpGet("user/{userId:int}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<ActionResult<IEnumerable<UserPagePermissionDto>>> GetUserPagePermissions(int userId)
    {
        try
        {
            _logger?.LogInformation("GetUserPagePermissions called for userId: {UserId}", userId);
            var userPermissions = await _context.UserPagePermissions
                .Include(upp => upp.PagePermission)
                .Include(upp => upp.User)
                .Where(upp => upp.UserId == userId)
                .ToListAsync();
            
            _logger?.LogInformation("GetUserPagePermissions: Found {Count} permissions for userId {UserId}", userPermissions.Count, userId);

            var dtos = userPermissions.Select(upp => new UserPagePermissionDto
            {
                Id = upp.Id,
                UserId = upp.UserId,
                UserName = upp.User.UserName,
                FullName = upp.User.FullName,
                PagePermissionId = upp.PagePermissionId,
                PageRoute = upp.PagePermission.PageRoute,
                PageName = upp.PagePermission.PageName,
                CanView = upp.CanView,
                CanCreate = upp.CanCreate,
                CanEdit = upp.CanEdit,
                CanDelete = upp.CanDelete
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetUserPagePermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving user page permissions", message = ex.Message });
        }
    }

    // GET: api/page-permissions/my-permissions
    [HttpGet("my-permissions")]
    public async Task<ActionResult<IEnumerable<UserPagePermissionDto>>> GetMyPagePermissions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Find user by FirebaseUID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == userId);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var userPermissions = await _context.UserPagePermissions
                .Include(upp => upp.PagePermission)
                .Where(upp => upp.UserId == user.UserId)
                .ToListAsync();

            var dtos = userPermissions.Select(upp => new UserPagePermissionDto
            {
                Id = upp.Id,
                UserId = upp.UserId,
                PagePermissionId = upp.PagePermissionId,
                PageRoute = upp.PagePermission.PageRoute,
                PageName = upp.PagePermission.PageName,
                CanView = upp.CanView,
                CanCreate = upp.CanCreate,
                CanEdit = upp.CanEdit,
                CanDelete = upp.CanDelete
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetMyPagePermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving my page permissions", message = ex.Message });
        }
    }

    // GET: api/page-permissions/check/{pageRoute}
    [HttpGet("check/{pageRoute}")]
    public async Task<ActionResult<object>> CheckPagePermission(string pageRoute)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Ok(new { canView = false, canCreate = false, canEdit = false, canDelete = false });
            }

            // Find user by FirebaseUID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == userId);

            if (user == null)
            {
                return Ok(new { canView = false, canCreate = false, canEdit = false, canDelete = false });
            }

            // Check if user is admin - admins have full access
            var isAdmin = await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == user.UserId && 
                    (ur.Role.RoleName == "Administrator" || ur.Role.RoleName == "Admin"));

            if (isAdmin)
            {
                return Ok(new { canView = true, canCreate = true, canEdit = true, canDelete = true });
            }

            // Find page permission
            var pagePermission = await _context.PagePermissions
                .FirstOrDefaultAsync(p => p.PageRoute == pageRoute && p.IsActive);

            if (pagePermission == null)
            {
                // If page not in permission system, allow access by default
                return Ok(new { canView = true, canCreate = false, canEdit = false, canDelete = false });
            }

            // Get user's permission for this page
            var userPagePermission = await _context.UserPagePermissions
                .FirstOrDefaultAsync(upp => upp.UserId == user.UserId && upp.PagePermissionId == pagePermission.Id);

            if (userPagePermission == null)
            {
                // No permission assigned, deny access
                return Ok(new { canView = false, canCreate = false, canEdit = false, canDelete = false });
            }

            return Ok(new
            {
                canView = userPagePermission.CanView,
                canCreate = userPagePermission.CanCreate,
                canEdit = userPagePermission.CanEdit,
                canDelete = userPagePermission.CanDelete
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CheckPagePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error checking page permission", message = ex.Message });
        }
    }

    // POST: api/page-permissions
    [HttpPost]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<ActionResult<PagePermissionDto>> CreatePagePermission([FromBody] CreatePagePermissionDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.PageRoute) || string.IsNullOrWhiteSpace(dto.PageName))
            {
                return BadRequest(new { error = "PageRoute and PageName are required" });
            }

            // Check if page route already exists
            var existing = await _context.PagePermissions
                .FirstOrDefaultAsync(p => p.PageRoute == dto.PageRoute);

            if (existing != null)
            {
                return BadRequest(new { error = "Page route already exists" });
            }

            var pagePermission = new PagePermission
            {
                PageRoute = dto.PageRoute,
                PageName = dto.PageName,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.PagePermissions.Add(pagePermission);
            await _context.SaveChangesAsync();

            var result = new PagePermissionDto
            {
                Id = pagePermission.Id,
                PageRoute = pagePermission.PageRoute,
                PageName = pagePermission.PageName,
                Description = pagePermission.Description,
                IsActive = pagePermission.IsActive
            };

            return CreatedAtAction(nameof(GetPagePermissions), new { id = pagePermission.Id }, result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreatePagePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating page permission", message = ex.Message });
        }
    }

    // POST: api/page-permissions/assign
    [HttpPost("assign")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<ActionResult> AssignPagePermissions([FromBody] AssignPagePermissionsDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Remove existing permissions for this user
            var existingPermissions = await _context.UserPagePermissions
                .Where(upp => upp.UserId == dto.UserId)
                .ToListAsync();

            _context.UserPagePermissions.RemoveRange(existingPermissions);

            // Add new permissions
            foreach (var perm in dto.Permissions)
            {
                var pagePermission = await _context.PagePermissions.FindAsync(perm.PagePermissionId);
                if (pagePermission == null)
                {
                    continue; // Skip invalid page permission
                }

                var userPagePermission = new UserPagePermission
                {
                    UserId = dto.UserId,
                    PagePermissionId = perm.PagePermissionId,
                    CanView = perm.CanView,
                    CanCreate = perm.CanCreate,
                    CanEdit = perm.CanEdit,
                    CanDelete = perm.CanDelete,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserPagePermissions.Add(userPagePermission);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Page permissions assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AssignPagePermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error assigning page permissions", message = ex.Message });
        }
    }

    // PUT: api/page-permissions/user/{userId}/page/{pagePermissionId}
    [HttpPut("user/{userId}/page/{pagePermissionId}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<ActionResult<UserPagePermissionDto>> UpdateUserPagePermission(
        int userId, 
        int pagePermissionId, 
        [FromBody] UpdateUserPagePermissionDto dto)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Verify page permission exists
            var pagePermission = await _context.PagePermissions.FindAsync(pagePermissionId);
            if (pagePermission == null)
            {
                return NotFound(new { error = "Page permission not found" });
            }

            // Try to find existing user page permission
            var userPagePermission = await _context.UserPagePermissions
                .Include(upp => upp.PagePermission)
                .Include(upp => upp.User)
                .FirstOrDefaultAsync(upp => upp.UserId == userId && upp.PagePermissionId == pagePermissionId);

            if (userPagePermission == null)
            {
                // Create new permission if it doesn't exist
                userPagePermission = new UserPagePermission
                {
                    UserId = userId,
                    PagePermissionId = pagePermissionId,
                    CanView = dto.CanView,
                    CanCreate = dto.CanCreate,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    CreatedAt = DateTime.UtcNow,
                    User = user,
                    PagePermission = pagePermission
                };

                _context.UserPagePermissions.Add(userPagePermission);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing permission
                userPagePermission.CanView = dto.CanView;
                userPagePermission.CanCreate = dto.CanCreate;
                userPagePermission.CanEdit = dto.CanEdit;
                userPagePermission.CanDelete = dto.CanDelete;
                userPagePermission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            if (userPagePermission == null)
            {
                return StatusCode(500, new { error = "Error saving user page permission" });
            }

            var result = new UserPagePermissionDto
            {
                Id = userPagePermission.Id,
                UserId = userPagePermission.UserId,
                UserName = userPagePermission.User.UserName,
                FullName = userPagePermission.User.FullName,
                PagePermissionId = userPagePermission.PagePermissionId,
                PageRoute = userPagePermission.PagePermission.PageRoute,
                PageName = userPagePermission.PagePermission.PageName,
                CanView = userPagePermission.CanView,
                CanCreate = userPagePermission.CanCreate,
                CanEdit = userPagePermission.CanEdit,
                CanDelete = userPagePermission.CanDelete
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateUserPagePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating user page permission", message = ex.Message });
        }
    }

    // DELETE: api/page-permissions/user/{userId}/page/{pagePermissionId}
    [HttpDelete("user/{userId}/page/{pagePermissionId}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> DeleteUserPagePermission(int userId, int pagePermissionId)
    {
        try
        {
            var userPagePermission = await _context.UserPagePermissions
                .FirstOrDefaultAsync(upp => upp.UserId == userId && upp.PagePermissionId == pagePermissionId);

            if (userPagePermission == null)
            {
                return NotFound(new { error = "User page permission not found" });
            }

            _context.UserPagePermissions.Remove(userPagePermission);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteUserPagePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting user page permission", message = ex.Message });
        }
    }
}

