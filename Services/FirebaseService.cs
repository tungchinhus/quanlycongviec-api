using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace quanlyfilesBE.Services;

public interface IFirebaseService
{
    Task<UserRecord> CreateUserAsync(string email, string password, string? displayName = null);
    Task SetCustomClaimsAsync(string uid, Dictionary<string, object> claims);
    Task<UserRecord?> GetUserAsync(string uid);
    Task DeleteUserAsync(string uid);
    Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
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
        if (FirebaseApp.DefaultInstance == null)
        {
            var firebaseConfig = _configuration.GetSection("Firebase");
            var credentialsPath = firebaseConfig["CredentialsPath"];
            var credentialsJson = firebaseConfig["CredentialsJson"];

            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath)
                });
                _logger.LogInformation("Firebase initialized with credentials file");
            }
            else if (!string.IsNullOrEmpty(credentialsJson))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentialsJson)
                });
                _logger.LogInformation("Firebase initialized with credentials JSON");
            }
            else
            {
                _logger.LogWarning("Firebase credentials not found. Firebase operations will fail.");
            }
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
}

