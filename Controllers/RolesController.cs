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
    // CHỈ LẤY TỪ DATABASE SQL SERVER - KHÔNG LẤY TỪ FIREBASE HAY CACHE
    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            // CHỈ LẤY TỪ DATABASE SQL SERVER - DÙNG ADO.NET RAW SQL HOÀN TOÀN
            // BYPASS ENTITY FRAMEWORK ĐỂ ĐẢM BẢO LẤY ĐÚNG DỮ LIỆU TỪ DB
            _logger?.LogInformation("GetAllRoles called - Lấy roles từ SQL Server database bằng ADO.NET RAW SQL");

            var roleDtos = new List<RoleDto>();
            var connection = _db.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (!wasOpen)
            {
                await _db.Database.OpenConnectionAsync();
            }
            
            try
            {
                // LẤY ROLES TRỰC TIẾP TỪ DATABASE BẰNG RAW SQL - KHÔNG QUA ENTITY FRAMEWORK
                // SQL SERVER SYNTAX - DÙNG SQUARE BRACKETS
                var roles = new List<(int RoleId, string RoleName, string? Description)>();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT [RoleId], [RoleName], [Description] FROM [Roles] ORDER BY [RoleId]";
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var roleId = reader.GetInt32(0);
                            var roleName = reader.GetString(1);
                            var description = reader.IsDBNull(2) ? null : reader.GetString(2);
                            
                            roles.Add((roleId, roleName, description));
                            
                            _logger?.LogInformation("Role from DB (ADO.NET) - RoleId: {RoleId}, RoleName: {RoleName}, Description: {Description}", 
                                roleId, roleName, description);
                        }
                    }
                }
                
                _logger?.LogInformation("Retrieved {Count} roles from database using ADO.NET RAW SQL", roles.Count);
                
                // Lấy permissions cho tất cả roles bằng raw SQL
                var rolePermissionsMap = new Dictionary<int, List<string>>();
                var roleIds = roles.Select(r => r.RoleId).ToList();
                
                foreach (var roleId in roleIds)
                {
                    rolePermissionsMap[roleId] = new List<string>();
                }
                
                if (roleIds.Any())
                {
                    using (var command = connection.CreateCommand())
                    {
                        // Lấy tất cả permissions cho tất cả roles trong một query
                        // SQL SERVER SYNTAX - DÙNG SQUARE BRACKETS
                        var roleIdsString = string.Join(",", roleIds);
                        command.CommandText = $@"
                            SELECT rp.[RoleId], p.[PermissionName] 
                            FROM [RolePermissions] rp 
                            INNER JOIN [Permissions] p ON rp.[PermissionId] = p.[PermissionId] 
                            WHERE rp.[RoleId] IN ({roleIdsString})
                            ORDER BY rp.[RoleId]";
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var rid = reader.GetInt32(0);
                                if (!reader.IsDBNull(1))
                                {
                                    var permName = reader.GetString(1);
                                    if (rolePermissionsMap.ContainsKey(rid))
                                    {
                                        rolePermissionsMap[rid].Add(permName);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Map roles sang DTOs với permissions - GIỮ NGUYÊN RoleId TỪ DB
                foreach (var role in roles)
                {
                    roleDtos.Add(new RoleDto
                    {
                        RoleId = role.RoleId, // GIỮ NGUYÊN RoleId TỪ DB - KHÔNG TRANSFORM
                        RoleName = role.RoleName, // GIỮ NGUYÊN RoleName TỪ DB
                        Description = role.Description, // GIỮ NGUYÊN Description TỪ DB
                        Permissions = rolePermissionsMap.GetValueOrDefault(role.RoleId, new List<string>())
                    });
                }
            }
            finally
            {
                if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            _logger?.LogInformation("Returning {Count} roles to client", roleDtos.Count);
            
            // Log chi tiết từng role DTO để debug
            foreach (var dto in roleDtos)
            {
                _logger?.LogInformation("Role DTO - RoleId: {RoleId}, RoleName: {RoleName}, Description: {Description}", 
                    dto.RoleId, dto.RoleName, dto.Description);
            }

            // Disable caching - luôn lấy dữ liệu mới từ database
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

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

