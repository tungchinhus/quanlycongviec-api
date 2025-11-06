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
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RolesController>? _logger;

    public RolesController(ApplicationDbContext db, ILogger<RolesController>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _db.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var roleDtos = roles.Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
            }).ToList();

            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllRoles: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving roles", message = ex.Message });
        }
    }

    // GET: api/roles/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        try
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound(new { error = "Role not found" });
            }

            var roleDto = new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
            };

            return Ok(roleDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetRoleById: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving role", message = ex.Message });
        }
    }

    // POST: api/roles
    [HttpPost]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(createDto.RoleName))
            {
                return BadRequest(new { error = "RoleName is required" });
            }

            // Check if role name already exists
            var existingRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == createDto.RoleName);

            if (existingRole != null)
            {
                return BadRequest(new { error = "Role name already exists" });
            }

            var role = new Role
            {
                RoleName = createDto.RoleName,
                Description = createDto.Description
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            var roleDto = new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                Permissions = new List<string>()
            };

            return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, roleDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateRole: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating role", message = ex.Message });
        }
    }

    // PUT: api/roles/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto updateDto)
    {
        try
        {
            var role = await _db.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound(new { error = "Role not found" });
            }

            // Check if role name already exists (if changing)
            if (!string.IsNullOrWhiteSpace(updateDto.RoleName) && updateDto.RoleName != role.RoleName)
            {
                var existingRole = await _db.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == updateDto.RoleName && r.RoleId != id);

                if (existingRole != null)
                {
                    return BadRequest(new { error = "Role name already exists" });
                }

                role.RoleName = updateDto.RoleName;
            }

            if (updateDto.Description != null)
            {
                role.Description = updateDto.Description;
            }

            await _db.SaveChangesAsync();

            // Reload with permissions
            await _db.Entry(role)
                .Collection(r => r.RolePermissions)
                .Query()
                .Include(rp => rp.Permission)
                .LoadAsync();

            var roleDto = new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
            };

            return Ok(roleDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateRole: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating role", message = ex.Message });
        }
    }

    // DELETE: api/roles/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        try
        {
            var role = await _db.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound(new { error = "Role not found" });
            }

            // Check if role is being used by any users
            var hasUsers = await _db.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (hasUsers)
            {
                return BadRequest(new { error = "Cannot delete role that is assigned to users" });
            }

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteRole: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting role", message = ex.Message });
        }
    }

    // GET: api/roles/{roleId}/permissions
    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        try
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return NotFound(new { error = "Role not found" });
            }

            var permissions = role.RolePermissions
                .Select(rp => new PermissionDto
                {
                    PermissionId = rp.Permission.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    Description = rp.Permission.Description
                })
                .ToList();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetRolePermissions: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving role permissions", message = ex.Message });
        }
    }

    // PUT: api/roles/{roleId}/permissions
    [HttpPut("{roleId}/permissions")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> AssignPermissionsToRole(int roleId, [FromBody] AssignPermissionsDto assignDto)
    {
        try
        {
            var role = await _db.Roles.FindAsync(roleId);

            if (role == null)
            {
                return NotFound(new { error = "Role not found" });
            }

            // Validate all permissions exist
            var validPermissionIds = await _db.Permissions
                .Where(p => assignDto.PermissionIds.Contains(p.PermissionId))
                .Select(p => p.PermissionId)
                .ToListAsync();

            if (validPermissionIds.Count != assignDto.PermissionIds.Count)
            {
                return BadRequest(new { error = "One or more permission IDs are invalid" });
            }

            // Remove existing role permissions
            var existingRolePermissions = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _db.RolePermissions.RemoveRange(existingRolePermissions);

            // Add new role permissions
            var newRolePermissions = assignDto.PermissionIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            }).ToList();

            _db.RolePermissions.AddRange(newRolePermissions);
            await _db.SaveChangesAsync();

            // Reload role with permissions
            await _db.Entry(role)
                .Collection(r => r.RolePermissions)
                .Query()
                .Include(rp => rp.Permission)
                .LoadAsync();

            var roleDto = new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
            };

            return Ok(roleDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AssignPermissionsToRole: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error assigning permissions to role", message = ex.Message });
        }
    }
}

