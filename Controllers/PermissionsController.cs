using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PermissionsController>? _logger;

    public PermissionsController(ApplicationDbContext db, ILogger<PermissionsController>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    // GET: api/permissions
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions()
    {
        try
        {
            var permissions = await _db.Permissions
                .OrderBy(p => p.PermissionName)
                .ToListAsync();

            var permissionDtos = permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description
            }).ToList();

            return Ok(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllPermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving permissions", message = ex.Message });
        }
    }

    // GET: api/permissions/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPermissionById(int id)
    {
        try
        {
            var permission = await _db.Permissions.FindAsync(id);

            if (permission == null)
            {
                return NotFound(new { error = "Permission not found" });
            }

            var permissionDto = new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description
            };

            return Ok(permissionDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetPermissionById: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving permission", message = ex.Message });
        }
    }

    // POST: api/permissions
    [HttpPost]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto createDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(createDto.PermissionName))
            {
                return BadRequest(new { error = "PermissionName is required" });
            }

            // Check if permission name already exists
            var existingPermission = await _db.Permissions
                .FirstOrDefaultAsync(p => p.PermissionName == createDto.PermissionName);

            if (existingPermission != null)
            {
                return BadRequest(new { error = "Permission name already exists" });
            }

            var permission = new Permission
            {
                PermissionName = createDto.PermissionName,
                Description = createDto.Description
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            var permissionDto = new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description
            };

            return CreatedAtAction(nameof(GetPermissionById), new { id = permission.PermissionId }, permissionDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreatePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating permission", message = ex.Message });
        }
    }

    // PUT: api/permissions/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionDto updateDto)
    {
        try
        {
            var permission = await _db.Permissions.FindAsync(id);

            if (permission == null)
            {
                return NotFound(new { error = "Permission not found" });
            }

            // Check if permission name already exists (if changing)
            if (!string.IsNullOrWhiteSpace(updateDto.PermissionName) && updateDto.PermissionName != permission.PermissionName)
            {
                var existingPermission = await _db.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionName == updateDto.PermissionName && p.PermissionId != id);

                if (existingPermission != null)
                {
                    return BadRequest(new { error = "Permission name already exists" });
                }

                permission.PermissionName = updateDto.PermissionName;
            }

            if (updateDto.Description != null)
            {
                permission.Description = updateDto.Description;
            }

            await _db.SaveChangesAsync();

            var permissionDto = new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description
            };

            return Ok(permissionDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdatePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating permission", message = ex.Message });
        }
    }

    // DELETE: api/permissions/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        try
        {
            var permission = await _db.Permissions.FindAsync(id);

            if (permission == null)
            {
                return NotFound(new { error = "Permission not found" });
            }

            // Check if permission is being used by any roles
            var hasRoles = await _db.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
            if (hasRoles)
            {
                return BadRequest(new { error = "Cannot delete permission that is assigned to roles" });
            }

            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeletePermission: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting permission", message = ex.Message });
        }
    }
}

