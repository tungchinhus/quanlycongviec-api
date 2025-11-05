using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

        public AuthController(ApplicationDbContext db, IConfiguration config, IFirebaseService firebaseService)
        {
            _db = db;
            _config = config;
            _firebaseService = firebaseService;
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
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserName == req.UserName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized();

        if (!user.IsActive)
            return Unauthorized("User account is inactive");

        var roleNames = user.UserRoles.Select(ur => ur.Role.RoleName).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.PermissionName)
            .Distinct()
            .ToList();

        var token = GenerateJwt(user, roleNames, permissions);
        return Ok(new { token, user = new { user.UserId, user.UserName, user.FullName, user.Email } });
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

            // Gán role mặc định "User" nếu có
            var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (defaultRole != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = defaultRole.RoleId,
                    AssignedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                // Reload user với roles
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);
            }
        }

        if (user == null || !user.IsActive)
            return Unauthorized("User account is inactive");

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
                user.FirebaseUID,
                roles = roleNames
            } 
        });
    }

    [HttpPost("login/firebase-token")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithFirebaseToken([FromBody] FirebaseTokenLoginRequest req)
    {
        if (string.IsNullOrEmpty(req.IdToken))
            return BadRequest("IdToken is required");

        try
        {
            // 1. Verify Firebase ID Token
            var decodedToken = await _firebaseService.VerifyIdTokenAsync(req.IdToken);
            var firebaseUid = decodedToken.Uid;

            // 2. Get user info from Firebase token claims
            var email = decodedToken.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            var name = decodedToken.Claims.TryGetValue("name", out var nameClaim) ? nameClaim?.ToString() : null;
            var emailVerified = decodedToken.Claims.TryGetValue("email_verified", out var emailVerifiedClaim) 
                && emailVerifiedClaim?.ToString() == "True";

            // 3. Get or create user in local DB
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);

            // Nếu user chưa tồn tại, tạo mới user
            if (user == null)
            {
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

                // Gán role mặc định "User" nếu có
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                if (defaultRole != null)
                {
                    _db.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = defaultRole.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();

                    // Reload user với roles
                    user = await _db.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);
                }
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

                // Reload user với roles
                user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);
            }

            if (user == null || !user.IsActive)
                return Unauthorized("User account is inactive");

            // 4. Get roles và permissions
            var roleNames = user.UserRoles.Select(ur => ur.Role.RoleName).Distinct().ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionName)
                .Distinct()
                .ToList();

            // 5. Get custom claims from Firebase (if any)
            var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
            if (firebaseUser?.CustomClaims != null && firebaseUser.CustomClaims.ContainsKey("roles"))
            {
                var firebaseRoles = firebaseUser.CustomClaims["roles"];
                if (firebaseRoles is System.Collections.IEnumerable rolesEnumerable)
                {
                    var firebaseRoleNames = rolesEnumerable.Cast<object>().Select(r => r.ToString()!).Where(r => !string.IsNullOrEmpty(r)).ToList();
                    // Sync roles from Firebase custom claims to DB if needed
                    // (Có thể thêm logic sync ở đây nếu cần)
                }
            }

            // 6. Generate JWT token
            var token = GenerateJwt(user, roleNames, permissions);
            return Ok(new
            {
                token,
                user = new
                {
                    user.UserId,
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
            return Unauthorized($"Invalid Firebase ID token: {ex.Message}");
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







