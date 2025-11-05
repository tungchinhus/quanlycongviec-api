using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<UsersController>? _logger;

    public UsersController(ApplicationDbContext db, IFirebaseService firebaseService, ILogger<UsersController>? logger = null)
    {
        _db = db;
        _firebaseService = firebaseService;
        _logger = logger;
    }

    // GET: api/users
    [HttpGet]
    [AllowAnonymous] // Cho phép xem danh sách users mà không cần auth (để test)
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.UserName.Contains(search) || 
                u.FullName != null && u.FullName.Contains(search) ||
                u.Email != null && u.Email.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = users.Select(u => new UserDto
        {
            UserId = u.UserId,
            UserName = u.UserName,
            FullName = u.FullName,
            Email = u.Email,
            FirebaseUID = u.FirebaseUID,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        }).ToList();

        return Ok(new
        {
            data = userDtos,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var userDto = new UserDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.UserName))
            return BadRequest("UserName is required");

        if (string.IsNullOrEmpty(dto.Email))
            return BadRequest("Email is required to create user on Firebase");

        if (string.IsNullOrEmpty(dto.Password))
            return BadRequest("Password is required to create user on Firebase");

        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            return BadRequest("Username already exists");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        if (!string.IsNullOrEmpty(dto.FirebaseUID) && await _db.Users.AnyAsync(u => u.FirebaseUID == dto.FirebaseUID))
            return BadRequest("FirebaseUID already exists");

        string firebaseUid = string.Empty;
        try
        {
            // 1. Tạo user trên Firebase Authentication
            var firebaseUser = await _firebaseService.CreateUserAsync(
                dto.Email,
                dto.Password,
                dto.FullName ?? dto.UserName
            );
            firebaseUid = firebaseUser.Uid;

            // 2. Lấy danh sách role names từ role ids
            var roleNames = new List<string>();
            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                var roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
                roleNames = roles.Select(r => r.RoleName).ToList();
            }

            // 3. Set custom claims với roles trên Firebase
            var claims = new Dictionary<string, object>
            {
                { "roles", roleNames }
            };
            if (!string.IsNullOrEmpty(dto.FullName))
            {
                claims["name"] = dto.FullName;
            }
            await _firebaseService.SetCustomClaimsAsync(firebaseUid, claims);

            // 4. Sau khi tạo thành công trên Firebase, đồng bộ user và roles xuống DB
            var user = new User
            {
                UserName = dto.UserName,
                FullName = dto.FullName,
                Email = dto.Email,
                FirebaseUID = firebaseUid,
                PasswordHash = string.Empty, // Không cần password hash vì dùng Firebase auth
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 5. Đồng bộ roles xuống DB
            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                var roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }

            // Reload với roles
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            var userDto = new UserDto
            {
                UserId = user!.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }
        catch (Exception ex)
        {
            // Nếu đã tạo trên Firebase nhưng lưu DB thất bại, cleanup Firebase user
            if (!string.IsNullOrEmpty(firebaseUid))
            {
                try
                {
                    await _firebaseService.DeleteUserAsync(firebaseUid);
                    _logger?.LogInformation("Firebase user cleaned up: {FirebaseUid}", firebaseUid);
                }
                catch (Exception deleteEx)
                {
                    _logger?.LogWarning(deleteEx, "Failed to cleanup Firebase user: {FirebaseUid}", firebaseUid);
                }
            }
            
            _logger?.LogError(ex, "Error creating user. Firebase UID: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error creating user: {ex.Message}");
        }
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        // Lấy thông tin user hiện tại từ token
        // JWT token có claim "sub" chứa UserId (theo JwtRegisteredClaimNames.Sub)
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        var currentUserId = int.TryParse(currentUserIdClaim, out var userId) ? userId : 0;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");
        var isManager = User.IsInRole("Manager");
        
        _logger?.LogInformation("UpdateUser - CurrentUserId: {CurrentUserId}, TargetId: {TargetId}, IsAdmin: {IsAdmin}, IsManager: {IsManager}", 
            currentUserId, id, isAdmin, isManager);

        // Kiểm tra quyền: Admin/Manager có thể update bất kỳ user nào, user chỉ có thể update chính mình
        if (!isAdmin && !isManager && currentUserId != id)
        {
            return Forbid("You can only update your own profile");
        }

        // Chỉ Admin/Manager mới có thể update roles và IsActive
        if ((dto.RoleIds != null || dto.IsActive.HasValue) && !isAdmin && !isManager)
        {
            return Forbid("Only Admin or Manager can update roles and active status");
        }

        if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
        {
            if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != id))
                return BadRequest("Username already exists");
            user.UserName = dto.UserName;
        }

        if (dto.FullName != null)
            user.FullName = dto.FullName;

        if (dto.Email != null)
            user.Email = dto.Email;

        // Chỉ Admin/Manager mới có thể thay đổi IsActive
        if (dto.IsActive.HasValue && (isAdmin || isManager))
        {
            user.IsActive = dto.IsActive.Value;
        }

        // Cập nhật roles nếu có (chỉ Admin/Manager)
        if (dto.RoleIds != null && (isAdmin || isManager))
        {
            // Xóa tất cả roles hiện tại
            var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == id);
            _db.UserRoles.RemoveRange(existingUserRoles);

            // Thêm roles mới
            var roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
            foreach (var role in roles)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = role.RoleId,
                    AssignedAt = DateTime.UtcNow
                });
            }

            // Nếu user có FirebaseUID, đồng bộ roles lên Firebase
            if (!string.IsNullOrEmpty(user.FirebaseUID))
            {
                try
                {
                    var roleNames = roles.Select(r => r.RoleName).ToList();
                    var claims = new Dictionary<string, object>
                    {
                        { "roles", roleNames }
                    };
                    if (!string.IsNullOrEmpty(user.FullName))
                    {
                        claims["name"] = user.FullName;
                    }
                    await _firebaseService.SetCustomClaimsAsync(user.FirebaseUID, claims);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to update Firebase custom claims for user {UserId}", user.UserId);
                    // Không throw error, chỉ log warning
                }
            }
        }

        await _db.SaveChangesAsync();

        // Reload với roles
        user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        var userDto = new UserDto
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/users/{id}/activate
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User activated successfully" });
    }

    // PATCH: api/users/{id}/deactivate
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User deactivated successfully" });
    }

    // POST: api/users (Firebase version - create user with Firebase Auth and set custom claims)
    // Note: This endpoint uses CreateUserWithFirebaseDto (name, email, password, roles as string array)
    // The existing POST /api/users uses UserCreateDto (different structure)
    [HttpPost("firebase")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUserWithFirebase([FromBody] CreateUserWithFirebaseDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            return BadRequest("Email and password are required");

        // Check if user already exists in local DB
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return BadRequest("Email already exists in local database");
        }

        string firebaseUid = string.Empty;
        try
        {
            // 1. Create user on Firebase Authentication
            var firebaseUser = await _firebaseService.CreateUserAsync(
                dto.Email,
                dto.Password,
                dto.Name
            );
            firebaseUid = firebaseUser.Uid;

            // Check if user already exists in DB by FirebaseUID (in case of sync issue)
            existingUser = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);
            if (existingUser != null)
            {
                // User already exists, just update and return
                return await SyncAndUpdateUser(existingUser, dto);
            }

            // 2. Set custom claims
            var claims = new Dictionary<string, object>
            {
                { "roles", dto.Roles ?? new List<string>() }
            };
            if (!string.IsNullOrEmpty(dto.Name))
            {
                claims["name"] = dto.Name;
            }
            await _firebaseService.SetCustomClaimsAsync(firebaseUid, claims);

            // 3. Create user in local DB
            var userName = dto.Email.Split('@')[0];
            var baseUserName = userName;
            var counter = 1;
            while (await _db.Users.AnyAsync(u => u.UserName == userName))
            {
                userName = $"{baseUserName}{counter}";
                counter++;
            }

            var user = new User
            {
                UserName = userName,
                FullName = dto.Name,
                Email = dto.Email,
                FirebaseUID = firebaseUid,
                PasswordHash = string.Empty, // No password needed for Firebase auth
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Assign roles in local DB
            if (dto.Roles != null && dto.Roles.Any())
            {
                var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }

            // Reload with roles
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            var userDto = new UserDto
            {
                UserId = user!.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }
        catch (Exception ex)
        {
            // Try to clean up Firebase user if DB creation failed
            if (!string.IsNullOrEmpty(firebaseUid))
            {
                try
                {
                    // Note: Firebase Admin SDK doesn't have direct delete method in UserRecord
                    // You might need to implement DeleteUserAsync in FirebaseService
                    _logger?.LogWarning("Failed to create user in DB, Firebase user {Uid} may need manual cleanup", firebaseUid);
                }
                catch { }
            }
            return BadRequest($"Error creating Firebase user: {ex.Message}");
        }
    }

    // Helper method to sync and update existing user
    private async Task<IActionResult> SyncAndUpdateUser(User existingUser, CreateUserWithFirebaseDto dto)
    {
        // Update user info
        if (!string.IsNullOrEmpty(dto.Name) && existingUser.FullName != dto.Name)
            existingUser.FullName = dto.Name;
        if (!string.IsNullOrEmpty(dto.Email) && existingUser.Email != dto.Email)
            existingUser.Email = dto.Email;

        // Update roles
        if (dto.Roles != null)
        {
            var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == existingUser.UserId);
            _db.UserRoles.RemoveRange(existingUserRoles);

            if (dto.Roles.Any())
            {
                var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = existingUser.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        // Reload with roles
        existingUser = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == existingUser.UserId);

        var userDto = new UserDto
        {
            UserId = existingUser!.UserId,
            UserName = existingUser.UserName,
            FullName = existingUser.FullName,
            Email = existingUser.Email,
            FirebaseUID = existingUser.FirebaseUID,
            IsActive = existingUser.IsActive,
            CreatedAt = existingUser.CreatedAt,
            Roles = existingUser.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // PUT: api/users/{userId}/roles
    [HttpPut("{userId}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRoles(int userId, [FromBody] UpdateUserRolesDto dto)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound();

        if (string.IsNullOrEmpty(user.FirebaseUID))
            return BadRequest("User does not have a Firebase UID");

        try
        {
            // 1. Update roles in DB
            var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == userId);
            _db.UserRoles.RemoveRange(existingUserRoles);

            if (dto.Roles != null && dto.Roles.Any())
            {
                var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();

            // 2. Set custom claims on Firebase
            var claims = new Dictionary<string, object>
            {
                { "roles", dto.Roles ?? new List<string>() }
            };
            if (!string.IsNullOrEmpty(user.FullName))
            {
                claims["name"] = user.FullName;
            }
            await _firebaseService.SetCustomClaimsAsync(user.FirebaseUID, claims);

            // Reload with roles
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var userDto = new UserDto
            {
                UserId = user!.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error updating user roles: {ex.Message}");
        }
    }

    // POST: api/users/{firebaseUid}/set-custom-claims
    [HttpPost("{firebaseUid}/set-custom-claims")]
    [AllowAnonymous]
    public async Task<IActionResult> SetCustomClaims(string firebaseUid, [FromBody] SetCustomClaimsDto dto)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        try
        {
            var claims = new Dictionary<string, object>
            {
                { "roles", dto.Roles }
            };
            if (!string.IsNullOrEmpty(dto.Name))
            {
                claims["name"] = dto.Name;
            }

            await _firebaseService.SetCustomClaimsAsync(firebaseUid, claims);

            return Ok(new { success = true, message = "Custom claims set successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error setting custom claims: {ex.Message}");
        }
    }

    // GET: api/users/test-auth
    // Endpoint để test authentication và roles
    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var allClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        
        return Ok(new
        {
            message = "Authentication successful",
            userId,
            userName,
            roles,
            isAdmin = User.IsInRole("Admin"),
            allClaims
        });
    }

    // GET: api/users/by-firebase-uid/{firebaseUid}
    [HttpGet("by-firebase-uid/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserByFirebaseUid(string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

        if (user == null)
            return NotFound();

        var userDto = new UserDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // POST: api/users/by-firebase-uid/{firebaseUid}/sync-roles
    // Đồng bộ roles từ Firebase Custom Claims xuống local DB
    [HttpPost("by-firebase-uid/{firebaseUid}/sync-roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncRolesFromFirebaseClaims(string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        // Tìm user theo FirebaseUID
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

        if (user == null)
            return NotFound("User not found in local database");

        // Lấy roles từ Firebase Custom Claims
        var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
        if (firebaseUser == null)
            return NotFound("User not found in Firebase");

        var roleNamesFromFirebase = new List<string>();
        if (firebaseUser.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
        {
            var firebaseRoles = firebaseUser.CustomClaims["roles"];
            if (firebaseRoles is System.Collections.IEnumerable rolesEnumerable)
            {
                roleNamesFromFirebase = rolesEnumerable
                    .Cast<object>()
                    .Select(r => r?.ToString())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r!)
                    .Distinct()
                    .ToList();
            }
        }

        // Thay thế toàn bộ roles của user theo Firebase (kể cả khi rỗng)
        var existing = _db.UserRoles.Where(ur => ur.UserId == user.UserId);
        _db.UserRoles.RemoveRange(existing);

        if (roleNamesFromFirebase.Any())
        {
            var roleEntities = await _db.Roles
                .Where(r => roleNamesFromFirebase.Contains(r.RoleName))
                .ToListAsync();

            foreach (var role in roleEntities)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = role.RoleId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        // Reload user với roles mới
        user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        var updatedDto = new UserDto
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(new { message = "Roles synced from Firebase successfully", user = updatedDto });
    }

    // PUT: api/users/by-firebase-uid/{firebaseUid}
    [HttpPut("by-firebase-uid/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateOrCreateUserByFirebaseUid(string firebaseUid, [FromBody] SyncUserFromFirebaseDto dto)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

        if (user == null)
        {
            // Create new user
            var userName = dto.Email?.Split('@')[0] ?? $"user_{firebaseUid.Substring(0, Math.Min(8, firebaseUid.Length))}";
            var baseUserName = userName;
            var counter = 1;
            while (await _db.Users.AnyAsync(u => u.UserName == userName))
            {
                userName = $"{baseUserName}{counter}";
                counter++;
            }

            user = new User
            {
                UserName = userName,
                FullName = dto.Name,
                Email = dto.Email,
                FirebaseUID = firebaseUid,
                PasswordHash = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Update existing user
            if (!string.IsNullOrEmpty(dto.Name) && user.FullName != dto.Name)
                user.FullName = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
                user.Email = dto.Email;
        }

        // Update roles
        if (dto.Roles != null)
        {
            var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == user.UserId);
            _db.UserRoles.RemoveRange(existingUserRoles);

            if (dto.Roles.Any())
            {
                var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        // Reload with roles
        user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        var userDto = new UserDto
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // POST: api/users/sync-from-firebase/{firebaseUid}
    // Đồng bộ user từ Firebase về Local DB (nếu user đã tồn tại trên Firebase nhưng chưa có trong DB)
    [HttpPost("sync-from-firebase/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncUserFromFirebase(string firebaseUid, [FromBody] SyncUserFromFirebaseDto? dto = null)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        try
        {
            // 1. Check if user already exists in local DB
            var existingUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            if (existingUser != null)
            {
                // User already exists, return existing user
                var existingDto = new UserDto
                {
                    UserId = existingUser.UserId,
                    UserName = existingUser.UserName,
                    FullName = existingUser.FullName,
                    Email = existingUser.Email,
                    FirebaseUID = existingUser.FirebaseUID,
                    IsActive = existingUser.IsActive,
                    CreatedAt = existingUser.CreatedAt,
                    Roles = existingUser.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                };
                return Ok(new { message = "User already exists in database", user = existingDto });
            }

            // 2. Get user info from Firebase
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            if (firebaseUser == null)
            {
                return NotFound("User not found in Firebase");
            }

            // 3. Create user in local DB
            var email = dto?.Email ?? firebaseUser.Email ?? string.Empty;
            var name = dto?.Name ?? firebaseUser.DisplayName ?? string.Empty;

            var userName = !string.IsNullOrEmpty(email) 
                ? email.Split('@')[0] 
                : $"user_{firebaseUid.Substring(0, Math.Min(8, firebaseUid.Length))}";
            
            var baseUserName = userName;
            var counter = 1;
            while (await _db.Users.AnyAsync(u => u.UserName == userName))
            {
                userName = $"{baseUserName}{counter}";
                counter++;
            }

            var user = new User
            {
                UserName = userName,
                FullName = name,
                Email = email,
                FirebaseUID = firebaseUid,
                PasswordHash = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 4. Assign roles from DTO or get from Firebase custom claims
            var rolesToAssign = dto?.Roles ?? new List<string>();
            
            // If no roles in DTO, try to get from Firebase custom claims (if available)
            if (!rolesToAssign.Any() && firebaseUser.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
            {
                var firebaseRoles = firebaseUser.CustomClaims["roles"];
                if (firebaseRoles is System.Collections.IEnumerable rolesEnumerable)
                {
                    rolesToAssign = rolesEnumerable.Cast<object>().Select(r => r.ToString()!).Where(r => !string.IsNullOrEmpty(r)).ToList();
                }
            }

            // If still no roles, assign default "User" role
            if (!rolesToAssign.Any())
            {
                rolesToAssign = new List<string> { "User" };
            }

            if (rolesToAssign.Any())
            {
                var roles = await _db.Roles.Where(r => rolesToAssign.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }

            // 5. Reload with roles
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            var userDto = new UserDto
            {
                UserId = user!.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            return Ok(new { message = "User synced successfully from Firebase", user = userDto });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing user from Firebase: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error syncing user from Firebase: {ex.Message}");
        }
    }

    // GET: api/users/set-role-by-email?email={email}&role={roleName}
    // Endpoint tạm thời để set role cho user theo email (chỉ dùng trong dev)
    [HttpGet("set-role-by-email")]
    [AllowAnonymous]
    public async Task<IActionResult> SetRoleByEmail([FromQuery] string email, [FromQuery] string role = "Admin")
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required");

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return NotFound($"User with email {email} not found");

        var roleEntity = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == role);
        if (roleEntity == null)
        {
            var availableRoles = await _db.Roles.Select(r => r.RoleName).ToListAsync();
            return BadRequest($"Role '{role}' not found. Available roles: {string.Join(", ", availableRoles)}");
        }

        // Kiểm tra xem user đã có role này chưa
        var hasRole = user.UserRoles.Any(ur => ur.RoleId == roleEntity.RoleId);
        if (hasRole)
        {
            return Ok(new { message = $"User already has role '{role}'", user = new UserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            }});
        }

        // Thêm role cho user
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = roleEntity.RoleId,
            AssignedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Nếu user có FirebaseUID, đồng bộ roles lên Firebase
        if (!string.IsNullOrEmpty(user.FirebaseUID))
        {
            try
            {
                // Reload để lấy roles mới
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                var roleNames = user!.UserRoles.Select(ur => ur.Role.RoleName).ToList();
                var claims = new Dictionary<string, object>
                {
                    { "roles", roleNames }
                };
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims["name"] = user.FullName;
                }
                if (!string.IsNullOrEmpty(user.FirebaseUID))
                {
                    await _firebaseService.SetCustomClaimsAsync(user.FirebaseUID, claims);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update Firebase custom claims for user {UserId}", user.UserId);
                // Không throw error, chỉ log warning
            }
        }

        // Reload với roles
        user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        if (user == null)
            return BadRequest("Failed to reload user after role update");

        var userDto = new UserDto
        {
            UserId = user!.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            FirebaseUID = user.FirebaseUID,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(new { message = $"Successfully set role '{role}' for user {email}", user = userDto });
    }
}

