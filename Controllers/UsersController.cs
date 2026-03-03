using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Helpers;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        try
        {
            // Log để debug
            _logger?.LogInformation("GetAllUsers called - Page: {Page}, PageSize: {PageSize}, Search: {Search}", page, pageSize, search ?? "null");

            // Đảm bảo page và pageSize hợp lệ
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100; // Tăng mặc định để lấy nhiều users hơn
            if (pageSize > 1000) pageSize = 1000; // Tăng giới hạn tối đa

            // CHỈ LẤY TỪ DATABASE - KHÔNG LẤY TỪ FIREBASE HAY HARDCODE
            var query = _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // Log tổng số users trước khi filter
            var totalUsersBeforeFilter = await _db.Users.CountAsync();
            _logger?.LogInformation("Total users in database (before filter): {Count}", totalUsersBeforeFilter);

            // CHỈ ÁP DỤNG SEARCH FILTER NẾU CÓ - KHÔNG FILTER THEO ISACTIVE HAY BẤT KỲ ĐIỀU KIỆN NÀO KHÁC
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.UserName.Contains(search) || 
                    u.FullName != null && u.FullName.Contains(search) ||
                    u.Email != null && u.Email.Contains(search));
                _logger?.LogInformation("Applied search filter: {Search}", search);
            }
            
            // KHÔNG FILTER THEO IsActive - LẤY TẤT CẢ USERS TỪ DB
            // KHÔNG LẤY TỪ FIREBASE - CHỈ LẤY TỪ SQL SERVER DATABASE

            var totalCount = await query.CountAsync();
            _logger?.LogInformation("Total users after filter: {Count}", totalCount);

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger?.LogInformation("Retrieved {Count} users for page {Page}", users.Count, page);

            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                FirebaseUID = u.FirebaseUID,
                IsActive = u.IsActive,
                IsDesigner = u.IsDesigner,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles?
                    .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
                    .Select(ur => ur.Role!.RoleName)
                    .Distinct()
                    .ToList() ?? new List<string>()
            }).ToList();

            var response = new
            {
                data = userDtos,
                totalCount,
                page,
                pageSize,
                totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0
            };

            _logger?.LogInformation("Returning {Count} users, totalCount: {TotalCount}, totalPages: {TotalPages}", 
                userDtos.Count, totalCount, response.totalPages);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllUsers: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving users", message = ex.Message });
        }
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
            IsDesigner = user.IsDesigner,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles?
                .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
                .Select(ur => ur.Role!.RoleName)
                .Distinct()
                .ToList() ?? new List<string>()
        };

        return Ok(userDto);
    }

    // POST: api/users
    // Endpoint này tự động detect format:
    // - Nếu có "roles" (array) -> không yêu cầu Admin, tự động chuyển đến CreateUserSimple
    // - Nếu có "roleIds" (array) -> yêu cầu Admin role
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] JsonElement requestBody)
    {
        // Kiểm tra format: nếu có "roles" (array) thì dùng CreateUserSimple (không cần Admin)
        if (requestBody.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
        {
            // Parse sang UserCreateSimpleDto và gọi CreateUserSimple
            var simpleDto = System.Text.Json.JsonSerializer.Deserialize<UserCreateSimpleDto>(
                requestBody.GetRawText(), 
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            
            if (simpleDto != null)
            {
                return await CreateUserSimple(simpleDto);
            }
        }
        
        // Nếu có roleIds hoặc không có roles, yêu cầu Admin role
        if (requestBody.TryGetProperty("roleIds", out var roleIdsElement) && roleIdsElement.ValueKind == JsonValueKind.Array)
        {
            // Format với roleIds - yêu cầu Administrator
            if (!User.Identity?.IsAuthenticated ?? true || !RoleHelper.IsAdministrator(User))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Administrator role required when using roleIds format" });
            }
        }
        else
        {
            // Không có roles và không có roleIds - yêu cầu Administrator (legacy format)
            if (!User.Identity?.IsAuthenticated ?? true || !RoleHelper.IsAdministrator(User))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Administrator role required. Use format with 'roles' array to create without Administrator role" });
            }
        }
        
        // Parse sang UserCreateDto và xử lý như cũ (format với roleIds)
        var dto = JsonSerializer.Deserialize<UserCreateDto>(requestBody.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
            return BadRequest("Invalid request format");
        
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
                CreatedAt = DateTimeHelper.NowVietnam()
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
                        AssignedAt = DateTimeHelper.NowVietnam()
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
                IsDesigner = user.IsDesigner,
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

        // Lấy thông tin user hiện tại từ token (FirebaseUID)
        var firebaseUID = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        
        User? currentUser = null;
        int currentUserId = 0;
        
        if (!string.IsNullOrEmpty(firebaseUID))
        {
            currentUser = await _db.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
            
            if (currentUser == null)
            {
                // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                                ?? User.FindFirst("email")?.Value;
                
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    currentUser = await _db.Users
                        .FirstOrDefaultAsync(u => u.Email == emailClaim);
                }
            }
            
            if (currentUser != null)
            {
                currentUserId = currentUser.UserId;
            }
        }
        
        var isAdmin = RoleHelper.IsAdministrator(User);
        var isManager = RoleHelper.IsManager(User);
        
        _logger?.LogInformation("UpdateUser - CurrentUserId: {CurrentUserId}, TargetId: {TargetId}, IsAdmin: {IsAdmin}, IsManager: {IsManager}", 
            currentUserId, id, isAdmin, isManager);

        // Kiểm tra quyền: Admin/Manager có thể update bất kỳ user nào, user chỉ có thể update chính mình
        if (!isAdmin && !isManager && currentUserId != id)
        {
            return StatusCode(403, new { error = "Forbidden", message = "You can only update your own profile" });
        }

        // Chỉ Admin/Manager mới có thể update roles và IsActive
        if ((dto.RoleIds != null || dto.IsActive.HasValue) && !isAdmin && !isManager)
        {
            return StatusCode(403, new { error = "Forbidden", message = "Only Admin or Manager can update roles and active status" });
        }

        if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
        {
            if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != id))
                return BadRequest("Username already exists");
            user.UserName = dto.UserName;
        }

        // Track changes để update Firebase
        bool fullNameChanged = false;
        bool emailChanged = false;

        if (dto.FullName != null)
        {
            fullNameChanged = user.FullName != dto.FullName;
            user.FullName = dto.FullName;
        }

        if (dto.Email != null)
        {
            emailChanged = user.Email != dto.Email;
            user.Email = dto.Email;
        }

        // Chỉ Admin/Manager mới có thể thay đổi IsActive và IsDesigner
        if (dto.IsActive.HasValue && (isAdmin || isManager))
        {
            user.IsActive = dto.IsActive.Value;
        }
        if (dto.IsDesigner.HasValue && (isAdmin || isManager))
        {
            user.IsDesigner = dto.IsDesigner.Value;
        }

        // Update user info trên Firebase nếu có thay đổi
        if (!string.IsNullOrEmpty(user.FirebaseUID) && (fullNameChanged || emailChanged))
        {
            try
            {
                await _firebaseService.UpdateUserAsync(
                    user.FirebaseUID,
                    emailChanged ? user.Email : null,
                    fullNameChanged ? user.FullName : null
                );
                _logger?.LogInformation("Updated Firebase user info for user {UserId}: Email={EmailChanged}, DisplayName={DisplayNameChanged}", 
                    user.UserId, emailChanged, fullNameChanged);

                // Nếu fullName thay đổi, cũng cần update custom claims với name mới
                if (fullNameChanged && !string.IsNullOrEmpty(user.FullName))
                {
                    // Lấy roles hiện tại từ DB để update custom claims
                    var currentRoles = await _db.UserRoles
                        .Where(ur => ur.UserId == user.UserId)
                        .Include(ur => ur.Role)
                        .Select(ur => ur.Role.RoleName)
                        .ToListAsync();

                    var claims = new Dictionary<string, object>
                    {
                        { "roles", currentRoles }
                    };
                    claims["name"] = user.FullName;
                    await _firebaseService.SetCustomClaimsAsync(user.FirebaseUID, claims);
                    _logger?.LogInformation("Updated Firebase custom claims with new name for user {UserId}", user.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update Firebase user info for user {UserId}", user.UserId);
                // Không throw error, chỉ log warning - DB đã được update
            }
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
                    AssignedAt = DateTimeHelper.NowVietnam()
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
            IsDesigner = user.IsDesigner,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            _logger?.LogInformation("DeleteUser called - UserId: {UserId}", id);

            // Load user với relationships
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                _logger?.LogWarning("User not found - UserId: {UserId}", id);
                return NotFound(new { error = "User not found", userId = id });
            }

            _logger?.LogInformation("Deleting user - UserId: {UserId}, UserName: {UserName}, Email: {Email}, FirebaseUID: {FirebaseUID}", 
                user.UserId, user.UserName, user.Email, user.FirebaseUID ?? "null");

            // 1. Xóa user trên Firebase nếu có FirebaseUID
            if (!string.IsNullOrEmpty(user.FirebaseUID))
            {
                try
                {
                    await _firebaseService.DeleteUserAsync(user.FirebaseUID);
                    _logger?.LogInformation("Firebase user deleted - FirebaseUID: {FirebaseUID}", user.FirebaseUID);
                }
                catch (Exception firebaseEx)
                {
                    _logger?.LogWarning(firebaseEx, "Failed to delete Firebase user - FirebaseUID: {FirebaseUID}. Continuing with local DB deletion.", 
                        user.FirebaseUID);
                    // Tiếp tục xóa trong Local DB ngay cả khi Firebase xóa thất bại
                }
            }

            // 2. Xóa UserRoles trước (cascade delete có thể xử lý, nhưng explicit để chắc chắn)
            if (user.UserRoles.Any())
            {
                _db.UserRoles.RemoveRange(user.UserRoles);
                _logger?.LogInformation("Removed {Count} UserRoles for user - UserId: {UserId}", 
                    user.UserRoles.Count, user.UserId);
            }

            // 3. Xóa user trong Local DB
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            _logger?.LogInformation("User deleted successfully - UserId: {UserId}, UserName: {UserName}", 
                user.UserId, user.UserName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting user - UserId: {UserId}, Error: {Message}", id, ex.Message);
            return StatusCode(500, new { error = "Error deleting user", message = ex.Message });
        }
    }

    // PATCH: api/users/{id}/activate
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
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
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User deactivated successfully" });
    }

    // POST: api/users/create-simple
    // POST: api/users/create (Alias - không yêu cầu Admin)
    // Tạo user đơn giản với format từ UI form (userName, fullName, email, password, role/roles)
    // Không yêu cầu Admin role
    // Hỗ trợ format: role: "User" hoặc roles: ["User"]
    [HttpPost("create-simple")]
    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUserSimple([FromBody] UserCreateSimpleDto dto)
    {
        if (string.IsNullOrEmpty(dto.UserName))
            return BadRequest("UserName is required");

        if (string.IsNullOrEmpty(dto.Email))
            return BadRequest("Email is required");

        if (string.IsNullOrEmpty(dto.Password))
            return BadRequest("Password is required");

        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            return BadRequest("Username already exists");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

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

            // 2. Lấy roles từ role name hoặc roles array
            var roleNames = new List<string>();
            if (dto.Roles != null && dto.Roles.Any())
            {
                // Ưu tiên roles array nếu có
                roleNames.AddRange(dto.Roles.Where(r => !string.IsNullOrWhiteSpace(r)));
            }
            else if (!string.IsNullOrEmpty(dto.Role))
            {
                // Nếu không có roles array, dùng Role string
                roleNames.Add(dto.Role);
            }
            // KHÔNG CÓ FALLBACK HARDCODE - NẾU KHÔNG CÓ ROLE, ĐỂ TRỐNG
            // Roles phải được chỉ định rõ ràng, không tự động gán mặc định
            
            // KHÔNG ĐẢM BẢO CÓ ÍT NHẤT 1 ROLE - USER CÓ THỂ KHÔNG CÓ ROLE

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

            // 4. Tạo user trong Local DB
            var user = new User
            {
                UserName = dto.UserName,
                FullName = dto.FullName,
                Email = dto.Email,
                FirebaseUID = firebaseUid,
                PasswordHash = string.Empty, // Không cần password hash vì dùng Firebase auth
                IsActive = true,
                CreatedAt = DateTimeHelper.NowVietnam()
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 5. Đồng bộ roles xuống DB
            var roles = await _db.Roles.Where(r => roleNames.Contains(r.RoleName)).ToListAsync();
            foreach (var role in roles)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = role.RoleId,
                    AssignedAt = DateTimeHelper.NowVietnam()
                });
            }
            await _db.SaveChangesAsync();

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
                IsDesigner = user.IsDesigner,
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
                CreatedAt = DateTimeHelper.NowVietnam()
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
                        AssignedAt = DateTimeHelper.NowVietnam()
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
                IsDesigner = user.IsDesigner,
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
                        AssignedAt = DateTimeHelper.NowVietnam()
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        // Reload with roles
        var reloadedUser = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == existingUser.UserId);

        if (reloadedUser == null)
            return BadRequest("Failed to reload user after update");

        existingUser = reloadedUser;

        var userDto = new UserDto
        {
            UserId = existingUser.UserId,
            UserName = existingUser.UserName,
            FullName = existingUser.FullName,
            Email = existingUser.Email,
            FirebaseUID = existingUser.FirebaseUID,
            IsActive = existingUser.IsActive,
            IsDesigner = existingUser.IsDesigner,
            CreatedAt = existingUser.CreatedAt,
            Roles = existingUser.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // PUT: api/users/{userId}/roles
    [HttpPut("{userId}/roles")]
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
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
                        AssignedAt = DateTimeHelper.NowVietnam()
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
                IsDesigner = user.IsDesigner,
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
            isAdmin = RoleHelper.IsAdministrator(User),
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
            IsDesigner = user.IsDesigner,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(userDto);
    }

    // GET: api/users/by-username/{username}
    // Tìm user theo username hoặc email và trả về email để đăng nhập Firebase
    [HttpGet("by-username/{usernameOrEmail}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserByUsernameOrEmail(string usernameOrEmail)
    {
        if (string.IsNullOrEmpty(usernameOrEmail))
            return BadRequest("Username or email is required");

        // Tìm user theo username hoặc email (case-insensitive)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => 
                u.UserName.ToLower() == usernameOrEmail.ToLower() || 
                (u.Email != null && u.Email.ToLower() == usernameOrEmail.ToLower()));

        if (user == null)
            return NotFound(new { message = "Tên đăng nhập hoặc email không tồn tại." });

        // Trả về email để frontend sử dụng đăng nhập Firebase
        // Nếu user không có email, trả về lỗi
        if (string.IsNullOrEmpty(user.Email))
            return BadRequest(new { message = "User không có email. Không thể đăng nhập." });

        return Ok(new { email = user.Email });
    }

    // GET: api/users/check-custom-claims/{firebaseUid}
    // Kiểm tra custom claims của một user trên Firebase
    [HttpGet("check-custom-claims/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckCustomClaims(string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        try
        {
            // Lấy thông tin user từ Firebase
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            if (firebaseUser == null)
                return NotFound($"Firebase user with UID {firebaseUid} not found");

            // Lấy thông tin user từ local DB
            var localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            // Parse custom claims từ Firebase
            var customClaims = firebaseUser.CustomClaims ?? new Dictionary<string, object>();
            var rolesFromFirebase = new List<string>();
            
            // Lấy roles từ customClaims.roles
            if (customClaims.ContainsKey("roles"))
            {
                var firebaseRoles = customClaims["roles"];
                if (firebaseRoles is System.Collections.IEnumerable rolesEnumerable && firebaseRoles != null)
                {
                    rolesFromFirebase = rolesEnumerable
                        .Cast<object>()
                        .Select(r => r?.ToString())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r!)
                        .ToList();
                }
                else if (firebaseRoles is string singleRole)
                {
                    rolesFromFirebase = new List<string> { singleRole };
                }
            }

            var rolesFromLocalDB = localUser?.UserRoles?.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>();

            return Ok(new
            {
                firebaseUID = firebaseUid,
                firebaseUser = new
                {
                    uid = firebaseUser.Uid,
                    email = firebaseUser.Email,
                    displayName = firebaseUser.DisplayName,
                    emailVerified = firebaseUser.EmailVerified,
                    disabled = firebaseUser.Disabled,
                    creationTime = firebaseUser.UserMetaData.CreationTimestamp,
                    lastSignInTime = firebaseUser.UserMetaData.LastSignInTimestamp
                },
                customClaims = customClaims.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                ),
                rolesFromFirebase,
                rolesFromLocalDB,
                rolesMatch = rolesFromFirebase.OrderBy(r => r).SequenceEqual(rolesFromLocalDB.OrderBy(r => r)),
                localUser = localUser != null ? new
                {
                    userId = localUser.UserId,
                    userName = localUser.UserName,
                    fullName = localUser.FullName,
                    email = localUser.Email,
                    isActive = localUser.IsActive,
                    roles = rolesFromLocalDB
                } : null,
                hasLocalUser = localUser != null
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking custom claims for Firebase UID: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error checking custom claims: {ex.Message}");
        }
    }

    // POST: api/users/by-firebase-uid/{firebaseUid}/sync-roles
    // Đồng bộ roles từ Firebase Custom Claims xuống local DB
    [HttpPost("by-firebase-uid/{firebaseUid}/sync-roles")]
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
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
                    AssignedAt = DateTimeHelper.NowVietnam()
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
            IsDesigner = user.IsDesigner,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(new { message = "Roles synced from Firebase successfully", user = updatedDto });
    }

    // PUT: api/users/by-firebase-uid/{firebaseUid}
    [HttpPut("by-firebase-uid/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateOrCreateUserByFirebaseUid(string firebaseUid, [FromBody] SyncUserFromFirebaseDto? dto)
    {
        try
        {
            _logger?.LogInformation("UpdateOrCreateUserByFirebaseUid called - FirebaseUID: {FirebaseUID}", firebaseUid);

            if (string.IsNullOrEmpty(firebaseUid))
            {
                _logger?.LogWarning("UpdateOrCreateUserByFirebaseUid: Firebase UID is required");
                return BadRequest("Firebase UID is required");
            }

            // Handle null DTO
            if (dto == null)
            {
                _logger?.LogWarning("UpdateOrCreateUserByFirebaseUid: DTO is null, creating with default values");
                dto = new SyncUserFromFirebaseDto
                {
                    Name = string.Empty,
                    Email = string.Empty,
                    Roles = new List<string>()
                };
            }

            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            if (user == null)
            {
                _logger?.LogInformation("User not found, creating new user with FirebaseUID: {FirebaseUID}", firebaseUid);
                // Create new user
                var userName = !string.IsNullOrEmpty(dto.Email) && dto.Email.Contains('@')
                    ? dto.Email.Split('@')[0]
                    : $"user_{firebaseUid.Substring(0, Math.Min(8, firebaseUid.Length))}";
                
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
                    CreatedAt = DateTimeHelper.NowVietnam()
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                _logger?.LogInformation("Created new user - UserId: {UserId}, UserName: {UserName}", user.UserId, user.UserName);
            }
            else
            {
                _logger?.LogInformation("User found, updating user - UserId: {UserId}", user.UserId);
                // Update existing user
                bool hasChanges = false;
                if (!string.IsNullOrEmpty(dto.Name) && user.FullName != dto.Name)
                {
                    user.FullName = dto.Name;
                    hasChanges = true;
                }
                if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
                {
                    user.Email = dto.Email;
                    hasChanges = true;
                }
                
                if (hasChanges)
                {
                    await _db.SaveChangesAsync();
                    _logger?.LogInformation("Updated user info - UserId: {UserId}", user.UserId);
                }
            }

            // Update roles
            if (dto.Roles != null && dto.Roles.Any())
            {
                _logger?.LogInformation("Updating roles for user - UserId: {UserId}, Roles: {Roles}", 
                    user.UserId, string.Join(", ", dto.Roles));
                
                var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == user.UserId);
                _db.UserRoles.RemoveRange(existingUserRoles);

                var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTimeHelper.NowVietnam()
                    });
                }
                await _db.SaveChangesAsync();
                _logger?.LogInformation("Updated {Count} roles for user - UserId: {UserId}", roles.Count, user.UserId);
            }

            // Reload with roles
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (user == null)
            {
                _logger?.LogError("Failed to reload user after update - UserId: {UserId}", user?.UserId ?? 0);
                return StatusCode(500, new { error = "Failed to reload user after update" });
            }

            var userDto = new UserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                FirebaseUID = user.FirebaseUID,
                IsActive = user.IsActive,
                IsDesigner = user.IsDesigner,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles?
                    .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
                    .Select(ur => ur.Role!.RoleName)
                    .Distinct()
                    .ToList() ?? new List<string>()
            };

            _logger?.LogInformation("Successfully updated/created user - UserId: {UserId}, FirebaseUID: {FirebaseUID}", 
                user.UserId, firebaseUid);

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateOrCreateUserByFirebaseUid - FirebaseUID: {FirebaseUID}, Error: {Message}", 
                firebaseUid, ex.Message);
            return StatusCode(500, new { error = "Error updating/creating user", message = ex.Message, stackTrace = ex.StackTrace });
        }
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
                    IsDesigner = existingUser.IsDesigner,
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
                CreatedAt = DateTimeHelper.NowVietnam()
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

            // KHÔNG GÁN ROLE MẶC ĐỊNH - CHỈ LẤY TỪ DATABASE HOẶC DTO
            // Nếu không có roles, user sẽ không có role (để trống)
            // Không có fallback hardcode

            if (rolesToAssign.Any())
            {
                var roles = await _db.Roles.Where(r => rolesToAssign.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTimeHelper.NowVietnam()
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
                IsDesigner = user.IsDesigner,
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

    // GET: api/users/check-custom-claims-by-email?email={email}
    // Kiểm tra custom claims của một user trên Firebase theo email
    [HttpGet("check-custom-claims-by-email")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckCustomClaimsByEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required");

        try
        {
            // Tìm user trong local DB theo email
            var localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (localUser == null || string.IsNullOrEmpty(localUser.FirebaseUID))
                return NotFound($"User with email {email} not found or does not have Firebase UID");

            // Sử dụng endpoint CheckCustomClaims với FirebaseUID
            return await CheckCustomClaims(localUser.FirebaseUID);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking custom claims for email: {Email}", email);
            return BadRequest($"Error checking custom claims: {ex.Message}");
        }
    }

    // GET: api/users/set-role-by-email?email={email}&role={roleName}
    // Endpoint tạm thời để set role cho user theo email (chỉ dùng trong dev)
    [HttpGet("set-role-by-email")]
    [AllowAnonymous]
    public async Task<IActionResult> SetRoleByEmail([FromQuery] string email, [FromQuery] string role = RoleHelper.AuthorizeRoles.Administrator)
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
                IsDesigner = user.IsDesigner,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            }});
        }

        // Thêm role cho user
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = roleEntity.RoleId,
            AssignedAt = DateTimeHelper.NowVietnam()
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
                _logger?.LogWarning(ex, "Failed to update Firebase custom claims for user {UserId}", user?.UserId ?? 0);
                // Không throw error, chỉ log warning
            }
        }

        // Reload với roles
        if (user != null)
        {
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);
        }

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
            IsDesigner = user.IsDesigner,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };

        return Ok(new { message = $"Successfully set role '{role}' for user {email}", user = userDto });
    }

    // POST: api/users/sync-roles-to-firebase/{firebaseUid}
    // Đồng bộ roles từ Local DB lên Firebase Custom Claims
    [HttpPost("sync-roles-to-firebase/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncRolesToFirebase(string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        try
        {
            // Lấy user từ Local DB
            var localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            if (localUser == null)
                return NotFound($"User with Firebase UID {firebaseUid} not found in local database");

            // Lấy roles từ Local DB
            var roleNames = localUser.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            // Set custom claims trên Firebase
            var claims = new Dictionary<string, object>
            {
                { "roles", roleNames }
            };
            if (!string.IsNullOrEmpty(localUser.FullName))
            {
                claims["name"] = localUser.FullName;
            }

            await _firebaseService.SetCustomClaimsAsync(firebaseUid, claims);

            // Lấy lại từ Firebase để verify
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            var firebaseRoles = new List<string>();
            if (firebaseUser?.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
            {
                var firebaseRolesObj = firebaseUser.CustomClaims["roles"];
                if (firebaseRolesObj is System.Collections.IEnumerable rolesEnumerable)
                {
                    firebaseRoles = rolesEnumerable
                        .Cast<object>()
                        .Select(r => r?.ToString())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r!)
                        .ToList();
                }
            }

            return Ok(new
            {
                message = "Roles synced to Firebase successfully",
                firebaseUID = firebaseUid,
                rolesFromLocalDB = roleNames,
                rolesFromFirebase = firebaseRoles,
                rolesMatch = roleNames.OrderBy(r => r).SequenceEqual(firebaseRoles.OrderBy(r => r))
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing roles to Firebase for UID: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error syncing roles to Firebase: {ex.Message}");
        }
    }

    // POST: api/users/sync-roles-from-firebase/{firebaseUid}
    // Đồng bộ roles từ Firebase Custom Claims xuống Local DB
    [HttpPost("sync-roles-from-firebase/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncRolesFromFirebase(string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        try
        {
            // Lấy user từ Firebase
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            if (firebaseUser == null)
                return NotFound($"Firebase user with UID {firebaseUid} not found");

            // Lấy user từ Local DB
            var localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            if (localUser == null)
                return NotFound($"User with Firebase UID {firebaseUid} not found in local database");

            // Parse roles từ Firebase custom claims
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

            // Xóa tất cả roles hiện tại
            var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == localUser.UserId);
            _db.UserRoles.RemoveRange(existingUserRoles);

            // Thêm roles mới từ Firebase
            if (roleNamesFromFirebase.Any())
            {
                var roles = await _db.Roles.Where(r => roleNamesFromFirebase.Contains(r.RoleName)).ToListAsync();
                foreach (var role in roles)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = localUser.UserId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTimeHelper.NowVietnam()
                    });
                }
            }

            await _db.SaveChangesAsync();

            // Reload để lấy roles mới
            localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == localUser.UserId);

            var rolesFromLocalDB = localUser!.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            return Ok(new
            {
                message = "Roles synced from Firebase successfully",
                firebaseUID = firebaseUid,
                rolesFromFirebase = roleNamesFromFirebase,
                rolesFromLocalDB = rolesFromLocalDB,
                rolesMatch = roleNamesFromFirebase.OrderBy(r => r).SequenceEqual(rolesFromLocalDB.OrderBy(r => r))
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing roles from Firebase for UID: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error syncing roles from Firebase: {ex.Message}");
        }
    }

    // POST: api/users/set-and-sync-role/{firebaseUid}?role={roleName}
    // Set role cho user cả trên Local DB và Firebase, đảm bảo đồng bộ
    [HttpPost("set-and-sync-role/{firebaseUid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SetAndSyncRole(string firebaseUid, [FromQuery] string role = RoleHelper.AuthorizeRoles.Administrator)
    {
        if (string.IsNullOrEmpty(firebaseUid))
            return BadRequest("Firebase UID is required");

        if (string.IsNullOrEmpty(role))
            return BadRequest("Role name is required");

        try
        {
            // 1. Lấy user từ Local DB
            var localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            if (localUser == null)
                return NotFound($"User with Firebase UID {firebaseUid} not found in local database");

            // 2. Kiểm tra role có tồn tại không
            var roleEntity = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == role);
            if (roleEntity == null)
            {
                var availableRoles = await _db.Roles.Select(r => r.RoleName).ToListAsync();
                return BadRequest($"Role '{role}' not found. Available roles: {string.Join(", ", availableRoles)}");
            }

            // 3. Kiểm tra user đã có role này chưa
            var hasRole = localUser.UserRoles.Any(ur => ur.RoleId == roleEntity.RoleId);
            if (!hasRole)
            {
                // Thêm role vào Local DB
                _db.UserRoles.Add(new UserRole
                {
                    UserId = localUser.UserId,
                    RoleId = roleEntity.RoleId,
                    AssignedAt = DateTimeHelper.NowVietnam()
                });
                await _db.SaveChangesAsync();
            }

            // 4. Reload để lấy tất cả roles
            localUser = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == localUser.UserId);

            // 5. Đồng bộ tất cả roles lên Firebase
            var allRoleNames = localUser!.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var claims = new Dictionary<string, object>
            {
                { "roles", allRoleNames }
            };
            if (!string.IsNullOrEmpty(localUser.FullName))
            {
                claims["name"] = localUser.FullName;
            }

            await _firebaseService.SetCustomClaimsAsync(firebaseUid, claims);

            // 6. Verify từ Firebase
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            var firebaseRoles = new List<string>();
            if (firebaseUser?.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
            {
                var firebaseRolesObj = firebaseUser.CustomClaims["roles"];
                if (firebaseRolesObj is System.Collections.IEnumerable rolesEnumerable)
                {
                    firebaseRoles = rolesEnumerable
                        .Cast<object>()
                        .Select(r => r?.ToString())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r!)
                        .ToList();
                }
            }

            return Ok(new
            {
                message = $"Successfully set role '{role}' and synced to Firebase",
                firebaseUID = firebaseUid,
                email = localUser.Email,
                rolesFromLocalDB = allRoleNames,
                rolesFromFirebase = firebaseRoles,
                rolesMatch = allRoleNames.OrderBy(r => r).SequenceEqual(firebaseRoles.OrderBy(r => r)),
                user = new UserDto
                {
                    UserId = localUser.UserId,
                    UserName = localUser.UserName,
                    FullName = localUser.FullName,
                    Email = localUser.Email,
                    FirebaseUID = localUser.FirebaseUID,
                    IsActive = localUser.IsActive,
                    IsDesigner = localUser.IsDesigner,
                    CreatedAt = localUser.CreatedAt,
                    Roles = allRoleNames
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting and syncing role for UID: {FirebaseUid}", firebaseUid);
            return BadRequest($"Error setting and syncing role: {ex.Message}");
        }
    }

    // GET: api/users/load-all-custom-claims
    // Load tất cả custom claims từ Firebase cho tất cả users
    [HttpGet("load-all-custom-claims")]
    [Authorize(Roles = RoleHelper.AuthorizeRoles.Administrator)]
    public async Task<IActionResult> LoadAllCustomClaims()
    {
        try
        {
            // Lấy tất cả users từ Firebase
            var firebaseUsers = await _firebaseService.ListAllUsersAsync();

            // Lấy tất cả users từ local DB
            var localUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            // Tạo dictionary để map FirebaseUID với local user
            var userMap = localUsers
                .Where(u => !string.IsNullOrEmpty(u.FirebaseUID))
                .ToDictionary(u => u.FirebaseUID!, u => u);

            var results = new List<object>();
            int usersWithClaimsCount = 0;

            foreach (var firebaseUser in firebaseUsers)
            {
                var customClaims = firebaseUser.CustomClaims ?? new Dictionary<string, object>();
                var hasClaims = customClaims.Any();
                if (hasClaims) usersWithClaimsCount++;
                
                // Tìm user tương ứng trong local DB
                var localUser = userMap.TryGetValue(firebaseUser.Uid, out var user) ? user : null;

                var claimData = new
                {
                    firebaseUid = firebaseUser.Uid,
                    email = firebaseUser.Email,
                    displayName = firebaseUser.DisplayName,
                    customClaims = customClaims.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                    ),
                    localUser = localUser != null ? new
                    {
                        userId = localUser.UserId,
                        userName = localUser.UserName,
                        fullName = localUser.FullName,
                        isActive = localUser.IsActive,
                        roles = localUser.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                    } : null,
                    hasLocalUser = localUser != null
                };

                results.Add(claimData);
            }

            return Ok(new
            {
                totalFirebaseUsers = firebaseUsers.Count,
                totalLocalUsers = localUsers.Count,
                usersWithClaims = usersWithClaimsCount,
                users = results
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading all custom claims");
            return BadRequest($"Error loading custom claims: {ex.Message}");
        }
    }

    // POST: api/users/sync-all-from-firebase
    // Đồng bộ TẤT CẢ users từ Firebase xuống Local DB
    [HttpPost("sync-all-from-firebase")]
    [AllowAnonymous] // Tạm thời cho phép không cần auth để test
    // [Authorize(Roles = "Admin")] // Uncomment sau khi test xong
    public async Task<IActionResult> SyncAllUsersFromFirebase()
    {
        _logger?.LogInformation("=== Starting sync-all-from-firebase endpoint ===");
        try
        {
            // 1. Lấy tất cả users từ Firebase
            _logger?.LogInformation("Fetching users from Firebase...");
            var firebaseUsers = await _firebaseService.ListAllUsersAsync();
            _logger?.LogInformation("Found {Count} users in Firebase", firebaseUsers?.Count ?? 0);
            
            if (firebaseUsers == null || firebaseUsers.Count == 0)
            {
                return Ok(new
                {
                    message = "No users found in Firebase",
                    syncedCount = 0,
                    createdCount = 0,
                    updatedCount = 0,
                    errors = new List<string>()
                });
            }

            var syncedUsers = new List<object>();
            var createdCount = 0;
            var updatedCount = 0;
            var errors = new List<string>();

            // 2. Lấy tất cả users từ local DB để map
            _logger?.LogInformation("Loading users from local database...");
            var localUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
            _logger?.LogInformation("Found {Count} users in local database", localUsers.Count);

            var localUserMap = localUsers
                .Where(u => !string.IsNullOrEmpty(u.FirebaseUID))
                .ToDictionary(u => u.FirebaseUID!, u => u);

            // 3. Lấy tất cả roles và permissions từ DB
            var allRoles = await _db.Roles.ToListAsync();
            var roleMap = allRoles.ToDictionary(r => r.RoleName, r => r);
            
            var allPermissions = await _db.Permissions.ToListAsync();
            var permissionMap = allPermissions.ToDictionary(p => p.PermissionName, p => p);

            // 4. Xử lý từng user từ Firebase
            foreach (var firebaseUser in firebaseUsers)
            {
                try
                {
                    // Parse roles và permissions từ Firebase custom claims
                    var roleNamesFromFirebase = new List<string>();
                    var permissionNamesFromFirebase = new List<string>();
                    
                    if (firebaseUser.CustomClaims != null)
                    {
                        // Parse roles
                        if (firebaseUser.CustomClaims.ContainsKey("roles"))
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
                        
                        // Parse permissions
                        if (firebaseUser.CustomClaims.ContainsKey("permissions"))
                        {
                            var firebasePermissions = firebaseUser.CustomClaims["permissions"];
                            if (firebasePermissions is System.Collections.IEnumerable permissionsEnumerable)
                            {
                                permissionNamesFromFirebase = permissionsEnumerable
                                    .Cast<object>()
                                    .Select(p => p?.ToString())
                                    .Where(p => !string.IsNullOrWhiteSpace(p))
                                    .Select(p => p!)
                                    .Distinct()
                                    .ToList();
                            }
                        }
                    }

                    // Kiểm tra user đã tồn tại trong local DB chưa
                    var localUser = localUserMap.TryGetValue(firebaseUser.Uid, out var existingUser) 
                        ? existingUser 
                        : null;

                    bool isNewUser = localUser == null;

                    if (localUser == null)
                    {
                        // Tạo user mới trong local DB
                        var email = firebaseUser.Email ?? string.Empty;
                        var userName = !string.IsNullOrEmpty(email) 
                            ? email.Split('@')[0] 
                            : $"user_{firebaseUser.Uid.Substring(0, Math.Min(8, firebaseUser.Uid.Length))}";

                        // Đảm bảo username unique
                        var baseUserName = userName;
                        var counter = 1;
                        while (await _db.Users.AnyAsync(u => u.UserName == userName))
                        {
                            userName = $"{baseUserName}{counter}";
                            counter++;
                        }

                        localUser = new User
                        {
                            UserName = userName,
                            FirebaseUID = firebaseUser.Uid,
                            Email = firebaseUser.Email,
                            FullName = firebaseUser.DisplayName,
                            PasswordHash = string.Empty, // Không cần password cho Firebase auth
                            IsActive = !firebaseUser.Disabled,
                            CreatedAt = DateTimeHelper.NowVietnam()
                        };

                        _db.Users.Add(localUser);
                        var savedCount = await _db.SaveChangesAsync();
                        _logger?.LogInformation("Created user {Email} with UserId {UserId}. Saved {Count} changes.", firebaseUser.Email, localUser.UserId, savedCount);
                        createdCount++;

                        // Reload để lấy UserId mới
                        localUser = await _db.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.UserId == localUser.UserId);
                    }
                    else
                    {
                        // Cập nhật thông tin user nếu cần
                        var needsUpdate = false;
                        if (localUser.Email != firebaseUser.Email)
                        {
                            localUser.Email = firebaseUser.Email;
                            needsUpdate = true;
                        }
                        if (localUser.FullName != firebaseUser.DisplayName)
                        {
                            localUser.FullName = firebaseUser.DisplayName;
                            needsUpdate = true;
                        }
                        if (localUser.IsActive != !firebaseUser.Disabled)
                        {
                            localUser.IsActive = !firebaseUser.Disabled;
                            needsUpdate = true;
                        }

                        if (needsUpdate)
                        {
                            var savedCount = await _db.SaveChangesAsync();
                            _logger?.LogInformation("Updated user {Email} with UserId {UserId}. Saved {Count} changes.", firebaseUser.Email, localUser.UserId, savedCount);
                            updatedCount++;
                        }
                    }

                    // 5. Đồng bộ roles từ Firebase custom claims
                    if (localUser != null)
                    {
                        // Xóa tất cả roles hiện tại
                        var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == localUser.UserId);
                        _db.UserRoles.RemoveRange(existingUserRoles);

                        // Thêm roles mới từ Firebase (tự động tạo Role nếu chưa tồn tại)
                        if (roleNamesFromFirebase.Any())
                        {
                            foreach (var roleName in roleNamesFromFirebase)
                            {
                                // Tự động tạo Role nếu chưa tồn tại
                                if (!roleMap.TryGetValue(roleName, out var role))
                                {
                                    role = new Role
                                    {
                                        RoleName = roleName,
                                        Description = $"Role synced from Firebase: {roleName}"
                                    };
                                    _db.Roles.Add(role);
                                    await _db.SaveChangesAsync();
                                    
                                    // Cập nhật roleMap
                                    roleMap[roleName] = role;
                                    _logger?.LogInformation("Created new role from Firebase: {RoleName}", roleName);
                                }

                                // Thêm UserRole
                                _db.UserRoles.Add(new UserRole
                                {
                                    UserId = localUser.UserId,
                                    RoleId = role.RoleId,
                                    AssignedAt = DateTimeHelper.NowVietnam()
                                });
                            }
                        }

                        var rolesSavedCount = await _db.SaveChangesAsync();
                        _logger?.LogInformation("Saved {Count} UserRoles for user {Email} (UserId: {UserId})", rolesSavedCount, firebaseUser.Email, localUser.UserId);

                        // Reload để lấy roles mới trước khi xử lý permissions
                        localUser = await _db.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.UserId == localUser.UserId);

                        // Gán permissions cho roles của user thông qua RolePermissions
                        if (permissionNamesFromFirebase.Any() && localUser != null && localUser.UserRoles.Any())
                        {
                            foreach (var permissionName in permissionNamesFromFirebase)
                            {
                                // Tự động tạo Permission nếu chưa tồn tại
                                if (!permissionMap.TryGetValue(permissionName, out var permission))
                                {
                                    permission = new Permission
                                    {
                                        PermissionName = permissionName,
                                        Description = $"Permission synced from Firebase: {permissionName}"
                                    };
                                    _db.Permissions.Add(permission);
                                    await _db.SaveChangesAsync();
                                    
                                    // Cập nhật permissionMap
                                    permissionMap[permissionName] = permission;
                                    _logger?.LogInformation("Created new permission from Firebase: {PermissionName}", permissionName);
                                }

                                // Gán permission cho tất cả roles của user
                                foreach (var userRole in localUser.UserRoles)
                                {
                                    // Kiểm tra xem RolePermission đã tồn tại chưa
                                    var rolePermissionExists = await _db.RolePermissions
                                        .AnyAsync(rp => rp.RoleId == userRole.RoleId && rp.PermissionId == permission.PermissionId);
                                    
                                    if (!rolePermissionExists)
                                    {
                                        _db.RolePermissions.Add(new RolePermission
                                        {
                                            RoleId = userRole.RoleId,
                                            PermissionId = permission.PermissionId
                                        });
                                    }
                                }
                            }
                            
                            var permissionsSavedCount = await _db.SaveChangesAsync();
                            _logger?.LogInformation("Saved {Count} RolePermissions for user {Email} (UserId: {UserId})", permissionsSavedCount, firebaseUser.Email, localUser?.UserId);
                        }
                    }

                    // Lấy permissions của user thông qua roles
                    var userPermissions = new List<string>();
                    if (localUser != null && localUser.UserRoles.Any())
                    {
                        var roleIds = localUser.UserRoles.Select(ur => ur.RoleId).ToList();
                        userPermissions = await _db.RolePermissions
                            .Where(rp => roleIds.Contains(rp.RoleId))
                            .Include(rp => rp.Permission)
                            .Select(rp => rp.Permission.PermissionName)
                            .Distinct()
                            .ToListAsync();
                    }

                    syncedUsers.Add(new
                    {
                        firebaseUid = firebaseUser.Uid,
                        email = firebaseUser.Email,
                        displayName = firebaseUser.DisplayName,
                        isNewUser = isNewUser,
                        roles = localUser?.UserRoles.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>(),
                        rolesFromFirebase = roleNamesFromFirebase,
                        permissions = userPermissions,
                        permissionsFromFirebase = permissionNamesFromFirebase
                    });
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error syncing user {firebaseUser.Uid} ({firebaseUser.Email}): {ex.Message}";
                    errors.Add(errorMsg);
                    _logger?.LogError(ex, "Error syncing user from Firebase: {FirebaseUid}", firebaseUser.Uid);
                }
            }

            var summary = new
            {
                totalRoles = await _db.Roles.CountAsync(),
                totalPermissions = await _db.Permissions.CountAsync(),
                totalUserRoles = await _db.UserRoles.CountAsync(),
                totalRolePermissions = await _db.RolePermissions.CountAsync()
            };
            
            _logger?.LogInformation("=== Sync completed: Created={Created}, Updated={Updated}, Errors={Errors}, TotalRoles={Roles}, TotalPermissions={Perms}, TotalUserRoles={UserRoles}, TotalRolePermissions={RolePerms} ===",
                createdCount, updatedCount, errors.Count, summary.totalRoles, summary.totalPermissions, summary.totalUserRoles, summary.totalRolePermissions);

            return Ok(new
            {
                message = "Sync completed",
                totalFirebaseUsers = firebaseUsers.Count,
                syncedCount = syncedUsers.Count,
                createdCount = createdCount,
                updatedCount = updatedCount,
                errorCount = errors.Count,
                errors = errors,
                summary = summary,
                users = syncedUsers
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing all users from Firebase: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            return BadRequest(new
            {
                error = "Error syncing users from Firebase",
                message = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    // GET: api/users/stats
    // Endpoint để kiểm tra số lượng users trong database
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var totalUsers = await _db.Users.CountAsync();
            var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
            var usersWithFirebase = await _db.Users.CountAsync(u => !string.IsNullOrEmpty(u.FirebaseUID));
            var usersWithRoles = await _db.Users
                .Include(u => u.UserRoles)
                .CountAsync(u => u.UserRoles.Any());

            // Lấy một vài users mẫu để kiểm tra
            var sampleUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Take(5)
                .Select(u => new
                {
                    u.UserId,
                    u.UserName,
                    u.Email,
                    u.FullName,
                    u.FirebaseUID,
                    u.IsActive,
                    rolesCount = u.UserRoles.Count
                })
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                usersWithFirebase,
                usersWithoutFirebase = totalUsers - usersWithFirebase,
                usersWithRoles,
                usersWithoutRoles = totalUsers - usersWithRoles,
                sampleUsers
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting user stats: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error getting user stats", message = ex.Message });
        }
    }

    // GET: api/users/all
    // Endpoint để lấy tất cả users không phân trang (để debug)
    // CHỈ LẤY TỪ DATABASE SQL SERVER - KHÔNG LẤY TỪ FIREBASE HAY HARDCODE
    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllUsersNoPagination()
    {
        try
        {
            // CHỈ LẤY TỪ DATABASE - KHÔNG CÓ FILTER NÀO
            var users = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                FirebaseUID = u.FirebaseUID,
                IsActive = u.IsActive,
                IsDesigner = u.IsDesigner,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles?
                    .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
                    .Select(ur => ur.Role!.RoleName)
                    .Distinct()
                    .ToList() ?? new List<string>()
            }).ToList();

            _logger?.LogInformation("GetAllUsersNoPagination: Returning {Count} users", userDtos.Count);

            return Ok(new
            {
                totalCount = userDtos.Count,
                users = userDtos
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllUsersNoPagination: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving users", message = ex.Message });
        }
    }

    // GET: api/users/test-sync
    // Endpoint test để kiểm tra từng bước
    [HttpGet("test-sync")]
    [AllowAnonymous]
    public async Task<IActionResult> TestSync()
    {
        try
        {
            var results = new List<object>();

            // 1. Test Firebase connection
            _logger?.LogInformation("Testing Firebase connection...");
            try
            {
                var firebaseUsers = await _firebaseService.ListAllUsersAsync();
                results.Add(new { step = "Firebase Connection", success = true, count = firebaseUsers?.Count ?? 0, message = "Firebase connected successfully" });
            }
            catch (Exception ex)
            {
                results.Add(new { step = "Firebase Connection", success = false, error = ex.Message, stackTrace = ex.StackTrace });
                return Ok(new { message = "Firebase test failed", results });
            }

            // 2. Test Database connection
            _logger?.LogInformation("Testing Database connection...");
            try
            {
                var userCount = await _db.Users.CountAsync();
                results.Add(new { step = "Database Connection", success = true, count = userCount, message = "Database connected successfully" });
            }
            catch (Exception ex)
            {
                results.Add(new { step = "Database Connection", success = false, error = ex.Message, stackTrace = ex.StackTrace });
                return Ok(new { message = "Database test failed", results });
            }

            // 3. Test create a simple user
            _logger?.LogInformation("Testing create user...");
            try
            {
                var testUser = new User
                {
                    UserName = $"test_user_{DateTimeHelper.NowVietnam().Ticks}",
                    Email = $"test_{DateTimeHelper.NowVietnam().Ticks}@test.com",
                    PasswordHash = "test",
                    IsActive = true,
                    CreatedAt = DateTimeHelper.NowVietnam()
                };
                _db.Users.Add(testUser);
                var savedCount = await _db.SaveChangesAsync();
                results.Add(new { step = "Create Test User", success = true, userId = testUser.UserId, savedCount = savedCount, message = "User created successfully" });
                
                // Delete test user
                _db.Users.Remove(testUser);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                results.Add(new { step = "Create Test User", success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }

            return Ok(new { message = "All tests completed", results });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in test sync: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}

