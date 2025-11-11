using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IFirebaseService _firebaseService;
        private readonly ILogger<AuthController>? _logger;

        public AuthController(ApplicationDbContext db, IConfiguration config, IFirebaseService firebaseService, ILogger<AuthController>? logger = null)
        {
            _db = db;
            _config = config;
            _firebaseService = firebaseService;
            _logger = logger;
        }

    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class FirebaseLoginRequest
    {
        public string FirebaseUID { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
    }

    public class FirebaseTokenLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == req.UserName))
            return BadRequest("Username already exists");

        var user = new User
        {
            UserName = req.UserName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FullName = req.FullName,
            Email = req.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.UserId, user.UserName });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // Cho phép đăng nhập bằng userName hoặc email
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => 
                u.UserName == req.UserName || 
                u.Email == req.UserName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid username/email or password");

        if (!user.IsActive)
            return Unauthorized("User account is inactive");

        var roleNames = user.UserRoles.Select(ur => ur.Role.RoleName).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.PermissionName)
            .Distinct()
            .ToList();

        var token = GenerateJwt(user, roleNames, permissions);
        return Ok(new { 
            token, 
            user = new { 
                user.UserId, 
                user.UserName, 
                user.FullName, 
                user.Email,
                roles = roleNames
            } 
        });
    }

    [HttpPost("login/firebase")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithFirebase([FromBody] FirebaseLoginRequest req)
    {
        if (string.IsNullOrEmpty(req.FirebaseUID))
            return BadRequest("FirebaseUID is required");

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.FirebaseUID == req.FirebaseUID);

        // Nếu user chưa tồn tại, tạo mới user
        if (user == null)
        {
            // Tạo username từ email hoặc FirebaseUID
            var userName = req.Email?.Split('@')[0] ?? $"user_{req.FirebaseUID.Substring(0, Math.Min(8, req.FirebaseUID.Length))}";
            
            // Đảm bảo username unique
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
                FirebaseUID = req.FirebaseUID,
                Email = req.Email,
                FullName = req.FullName,
                PasswordHash = string.Empty, // Không cần password cho Firebase auth
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

                // KHÔNG GÁN ROLE MẶC ĐỊNH - CHỈ LẤY TỪ DATABASE
                // Roles phải được gán thủ công qua API hoặc từ Firebase custom claims
                // Không có fallback hardcode

                // Reload user với roles để đảm bảo load đầy đủ dữ liệu
                var newUserId = user.UserId;
                _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == newUserId);
        }
        else
        {
            // Cập nhật thông tin nếu có thay đổi
            bool hasChanges = false;
            if (!string.IsNullOrEmpty(req.Email) && user.Email != req.Email)
            {
                user.Email = req.Email;
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(req.FullName) && user.FullName != req.FullName)
            {
                user.FullName = req.FullName;
                hasChanges = true;
            }
            if (hasChanges)
            {
                await _db.SaveChangesAsync();
            }

            // Reload user với roles để đảm bảo load đầy đủ dữ liệu
            var userId = user.UserId;
            _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        if (user == null || !user.IsActive)
            return Unauthorized("User account is inactive");

        var roleNames = user.UserRoles?.Select(ur => ur.Role?.RoleName)
            .Where(rn => !string.IsNullOrEmpty(rn))
            .Select(r => r!)
            .Distinct()
            .ToList() ?? new List<string>();
        
        var permissions = user.UserRoles?
            .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
            .Select(rp => rp.Permission?.PermissionName)
            .Where(pn => !string.IsNullOrEmpty(pn))
            .Select(p => p!)
            .Distinct()
            .ToList() ?? new List<string>();

        var token = GenerateJwt(user, roleNames, permissions);
        return Ok(new { 
            token, 
            user = new { 
                user.UserId, 
                user.UserName, 
                user.FullName, 
                user.Email,
                user.FirebaseUID,
                roles = roleNames
            } 
        });
    }

    [HttpPost("login/firebase-token")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithFirebaseToken([FromBody] FirebaseTokenLoginRequest? req)
    {
        try
        {
            _logger?.LogInformation("LoginWithFirebaseToken called");
            
            if (req == null || string.IsNullOrEmpty(req.IdToken))
            {
                _logger?.LogWarning("LoginWithFirebaseToken: IdToken is required");
                return BadRequest("IdToken is required");
            }
            
            // 1. Verify Firebase ID Token
            _logger?.LogInformation("Verifying Firebase ID token...");
            var decodedToken = await _firebaseService.VerifyIdTokenAsync(req.IdToken);
            var firebaseUid = decodedToken.Uid;
            _logger?.LogInformation("Firebase token verified - UID: {FirebaseUID}", firebaseUid);

            // 2. Get user info from Firebase token claims
            var email = decodedToken.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            var name = decodedToken.Claims.TryGetValue("name", out var nameClaim) ? nameClaim?.ToString() : null;
            var emailVerified = decodedToken.Claims.TryGetValue("email_verified", out var emailVerifiedClaim) 
                && emailVerifiedClaim?.ToString() == "True";

            // 3. Get or create user in local DB
            _logger?.LogInformation("Looking up user in local DB - FirebaseUID: {FirebaseUID}", firebaseUid);
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            // Nếu user chưa tồn tại, tạo mới user
            if (user == null)
            {
                _logger?.LogInformation("User not found in local DB, creating new user - FirebaseUID: {FirebaseUID}", firebaseUid);
                // Tạo username từ email hoặc FirebaseUID
                var userName = email?.Split('@')[0] ?? $"user_{firebaseUid.Substring(0, Math.Min(8, firebaseUid.Length))}";

                // Đảm bảo username unique
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
                    FirebaseUID = firebaseUid,
                    Email = email,
                    FullName = name,
                    PasswordHash = string.Empty, // Không cần password cho Firebase auth
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                _logger?.LogInformation("Created new user - UserId: {UserId}, UserName: {UserName}, Email: {Email}", 
                    user.UserId, user.UserName, user.Email);

                // KHÔNG GÁN ROLE MẶC ĐỊNH - CHỈ LẤY TỪ DATABASE
                // Roles phải được gán thủ công qua API hoặc từ Firebase custom claims
                // Không có fallback hardcode

                // Reload user với roles để đảm bảo load đầy đủ dữ liệu
                var newUserId = user.UserId;
                _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == newUserId);
            }
            else
            {
                // Cập nhật thông tin nếu có thay đổi
                bool hasChanges = false;
                if (!string.IsNullOrEmpty(email) && user.Email != email)
                {
                    user.Email = email;
                    hasChanges = true;
                }
                if (!string.IsNullOrEmpty(name) && user.FullName != name)
                {
                    user.FullName = name;
                    hasChanges = true;
                }
                if (hasChanges)
                {
                    await _db.SaveChangesAsync();
                }
            }

            // Reload user với roles để đảm bảo load đầy đủ dữ liệu
            // Detach user hiện tại để tránh tracking issues và đảm bảo load fresh data
            var userId = user!.UserId;
            _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || !user.IsActive)
                return Unauthorized("User account is inactive");

            // 4. Get roles và permissions từ Local DB
            var roleNames = user.UserRoles?.Select(ur => ur.Role?.RoleName)
                .Where(rn => !string.IsNullOrEmpty(rn))
                .Select(r => r!)
                .Distinct()
                .ToList() ?? new List<string>();
            
            var permissions = user.UserRoles?
                .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
                .Select(rp => rp.Permission?.PermissionName)
                .Where(pn => !string.IsNullOrEmpty(pn))
                .Select(p => p!)
                .Distinct()
                .ToList() ?? new List<string>();

            // 5. Get custom claims from Firebase và sync nếu Local DB không có roles
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            var firebaseRoleNames = new List<string>();
            
            if (firebaseUser?.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
            {
                var firebaseRoles = firebaseUser.CustomClaims["roles"];
                if (firebaseRoles is System.Collections.IEnumerable rolesEnumerable && firebaseRoles != null)
                {
                    firebaseRoleNames = rolesEnumerable
                        .Cast<object>()
                        .Select(r => r?.ToString())
                        .Where(r => !string.IsNullOrEmpty(r))
                        .Select(r => r!)
                        .Distinct()
                        .ToList();
                }
            }

            // 6. Nếu Local DB không có roles nhưng Firebase có, sync từ Firebase xuống Local DB
            if (!roleNames.Any() && firebaseRoleNames.Any())
            {
                // Xóa tất cả roles hiện tại (nếu có)
                var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == user.UserId);
                _db.UserRoles.RemoveRange(existingUserRoles);

                // Thêm roles từ Firebase
                var roles = await _db.Roles.Where(r => firebaseRoleNames.Contains(r.RoleName)).ToListAsync();
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

                // Reload user với roles mới
                _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                // Cập nhật roleNames và permissions từ user mới
                roleNames = user?.UserRoles?.Select(ur => ur.Role?.RoleName)
                    .Where(rn => !string.IsNullOrEmpty(rn))
                    .Select(r => r!)
                    .Distinct()
                    .ToList() ?? new List<string>();
                
                permissions = user?.UserRoles?
                    .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
                    .Select(rp => rp.Permission?.PermissionName)
                    .Where(pn => !string.IsNullOrEmpty(pn))
                    .Select(p => p!)
                    .Distinct()
                    .ToList() ?? new List<string>();
            }
            // Nếu cả 2 đều có roles, ưu tiên Local DB (vì Local DB là source of truth)
            // Nếu Local DB không có nhưng Firebase cũng không có, giữ nguyên (mảng rỗng)

            // 7. Generate JWT token
            var token = GenerateJwt(user!, roleNames, permissions);
            return Ok(new
            {
                token,
                user = new
                {
                    user!.UserId,
                    user.UserName,
                    user.FullName,
                    user.Email,
                    user.FirebaseUID,
                    roles = roleNames,
                    emailVerified
                }
            });
        }
        catch (Exception ex)
        {
            // Log chi tiết lỗi để debug
            _logger?.LogError(ex, "Error in LoginWithFirebaseToken: {Message}, StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            
            // Return 500 với thông tin chi tiết hơn để debug
            return StatusCode(500, new { 
                error = "Error during Firebase token login", 
                message = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            name = User.Identity?.Name,
            roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
            permissions = User.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList()
        });
    }

    private string GenerateJwt(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["ExpiryMinutes"] ?? "120")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}







