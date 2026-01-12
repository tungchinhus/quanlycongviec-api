using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace quanlyfilesBE.Services;

public interface IFirebaseService
{
    Task<UserRecord> CreateUserAsync(string email, string password, string? displayName = null);
    Task<UserRecord> UpdateUserAsync(string uid, string? email = null, string? displayName = null);
    Task SetCustomClaimsAsync(string uid, Dictionary<string, object> claims);
    Task<UserRecord?> GetUserAsync(string uid);
    Task DeleteUserAsync(string uid);
    Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
    Task<List<UserRecord>> ListAllUsersAsync(int maxResults = 1000);
}

public class FirebaseService : IFirebaseService
{
    private readonly ILogger<FirebaseService> _logger;
    private readonly IConfiguration _configuration;

    public FirebaseService(ILogger<FirebaseService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        // Kiểm tra đã khởi tạo chưa
        if (FirebaseApp.DefaultInstance != null)
        {
            _logger.LogInformation("FirebaseApp already initialized");
            return;
        }

        try
        {
            var firebaseConfig = _configuration.GetSection("Firebase");
            var credentialsPath = firebaseConfig["CredentialsPath"];
            var credentialsJson = firebaseConfig["CredentialsJson"];

            // Option 1: Sử dụng credentialsPath từ config
            if (!string.IsNullOrEmpty(credentialsPath))
            {
                // Nếu là relative path, tìm trong project directory
                if (!Path.IsPathRooted(credentialsPath))
                {
                    var projectDir = Directory.GetCurrentDirectory();
                    var fullPath = Path.Combine(projectDir, credentialsPath);
                    
                    if (File.Exists(fullPath))
                    {
                        credentialsPath = fullPath;
                    }
                    else if (File.Exists(credentialsPath))
                    {
                        // Giữ nguyên relative path nếu file tồn tại
                    }
                    else
                    {
                        // Fallback: thử tìm trong project root
                        credentialsPath = Path.Combine(projectDir, "service-account-key.json");
                    }
                }

                if (File.Exists(credentialsPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialsPath)
                    });
                    _logger.LogInformation("✅ Firebase initialized with credentials file: {Path}", credentialsPath);
                    return;
                }
                else
                {
                    _logger.LogWarning("Credentials file not found at path: {Path}", credentialsPath);
                }
            }

            // Option 2: Sử dụng credentialsJson từ config
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentialsJson)
                });
                _logger.LogInformation("✅ Firebase initialized with credentials JSON");
                return;
            }

            // Option 3: Fallback - tìm service-account-key.json trong project root
            var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "service-account-key.json");
            if (File.Exists(defaultPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(defaultPath)
                });
                _logger.LogInformation("✅ Firebase initialized with default credentials file: {Path}", defaultPath);
                return;
            }

            // Nếu không tìm thấy credentials
            _logger.LogError("❌ Firebase credentials not found. Tried paths: {Path1}, {Path2}", 
                credentialsPath ?? "null", defaultPath);
            throw new FileNotFoundException(
                $"Firebase credentials not found. Please ensure 'service-account-key.json' exists in the project root or configure Firebase:CredentialsPath in appsettings.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error initializing Firebase: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<UserRecord> CreateUserAsync(string email, string password, string? displayName = null)
    {
        try
        {
            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                EmailVerified = false
            });

            _logger.LogInformation("Firebase user created: {Uid}", userRecord.Uid);
            return userRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Firebase user");
            throw;
        }
    }

    public async Task<UserRecord> UpdateUserAsync(string uid, string? email = null, string? displayName = null)
    {
        try
        {
            var args = new UserRecordArgs
            {
                Uid = uid
            };

            if (email != null)
            {
                args.Email = email;
            }

            if (displayName != null)
            {
                args.DisplayName = displayName;
            }

            var userRecord = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
            _logger.LogInformation("Firebase user updated: {Uid}, Email: {Email}, DisplayName: {DisplayName}", 
                uid, email ?? "unchanged", displayName ?? "unchanged");
            return userRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Firebase user: {Uid}", uid);
            throw;
        }
    }

    public async Task SetCustomClaimsAsync(string uid, Dictionary<string, object> claims)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
            _logger.LogInformation("Custom claims set for user: {Uid}", uid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting custom claims for user: {Uid}", uid);
            throw;
        }
    }

    public async Task<UserRecord?> GetUserAsync(string uid)
    {
        try
        {
            return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Firebase user: {Uid}", uid);
            return null;
        }
    }

    public async Task DeleteUserAsync(string uid)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            _logger.LogInformation("Firebase user deleted: {Uid}", uid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Firebase user: {Uid}", uid);
            throw;
        }
    }

    public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            _logger.LogInformation("Firebase ID token verified for user: {Uid}", decodedToken.Uid);
            return decodedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Firebase ID token");
            throw;
        }
    }

    public async Task<List<UserRecord>> ListAllUsersAsync(int maxResults = 1000)
    {
        try
        {
            var users = new List<UserRecord>();
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(new ListUsersOptions());

            await foreach (var user in pagedEnumerable)
            {
                users.Add(user);
                if (users.Count >= maxResults)
                {
                    break;
                }
            }

            _logger.LogInformation("Listed {Count} Firebase users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Firebase users");
            throw;
        }
    }
}

