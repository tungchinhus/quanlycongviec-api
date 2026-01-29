using System.Runtime.InteropServices;
using System.Text;

namespace syncToServer;

/// <summary>
/// Provides secure network share access using Windows P/Invoke (WNetAddConnection2).
/// This is the recommended Microsoft approach for Windows Services to access password-protected network shares.
/// </summary>
public class NetworkShareAccess : IDisposable
{
    private bool _isConnected = false;
    private string _networkPath = string.Empty;

    #region P/Invoke Declarations

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        ref NETRESOURCE netResource,
        string password,
        string username,
        int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(
        string name,
        int flags,
        bool force);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string lpLocalName;
        public string lpRemoteName;
        public string lpComment;
        public string lpProvider;
    }

    private const int RESOURCETYPE_DISK = 0x00000001;
    private const int CONNECT_UPDATE_PROFILE = 0x00000001;
    private const int CONNECT_TEMPORARY = 0x00000004;

    #endregion

    /// <summary>
    /// Connects to a network share using the provided credentials.
    /// </summary>
    /// <param name="networkPath">UNC path (e.g., \\Server\Share)</param>
    /// <param name="username">Username for authentication</param>
    /// <param name="password">Password for authentication</param>
    /// <param name="domain">Domain (optional, can be empty)</param>
    /// <param name="lastError">Output: Windows error code when connection fails</param>
    /// <returns>True if connection successful, false otherwise</returns>
    public bool Connect(string networkPath, string username, string password, string domain, out int lastError)
    {
        lastError = 0;

        if (_isConnected && _networkPath.Equals(networkPath, StringComparison.OrdinalIgnoreCase))
        {
            return true; // Already connected to this path
        }

        // Disconnect existing connection if any
        if (_isConnected)
        {
            Disconnect();
        }

        // Ensure UNC path is normalized (no trailing slash for root share)
        string normalizedPath = networkPath?.TrimEnd('\\') ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedPath))
        {
            lastError = 87; // ERROR_INVALID_PARAMETER
            return false;
        }

        // Clear any existing connection to this path (avoids ERROR_SESSION_CREDENTIAL_CONFLICT 1219)
        WNetCancelConnection2(normalizedPath, 0, true);

        var netResource = new NETRESOURCE
        {
            dwType = RESOURCETYPE_DISK,
            lpRemoteName = normalizedPath
        };

        // Try different username formats: domain\user, then user@domain, then plain user
        string[] usernameFormats = string.IsNullOrWhiteSpace(domain)
            ? new[] { username }
            : new[] { $"{domain}\\{username}", $"{username}@{domain}", username };

        foreach (string fullUsername in usernameFormats)
        {
            int result = WNetAddConnection2(
                ref netResource,
                password ?? "",
                fullUsername,
                CONNECT_TEMPORARY);

            if (result == 0)
            {
                _isConnected = true;
                _networkPath = normalizedPath;
                return true;
            }

            lastError = result;
            // If credential conflict (1219), disconnect and retry with next format
            if (result != 1219)
                break;
        }

        return false;
    }

    /// <summary>
    /// Returns a human-readable message for WNetAddConnection2 error codes.
    /// </summary>
    public static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            5 => "Access denied (kiểm tra quyền share và user).",
            53 => "Network path not found (kiểm tra IP/hostname và tên share LOCALSITE).",
            86 => "Invalid password (mật khẩu sai).",
            87 => "Invalid parameter (kiểm tra đường dẫn UNC).",
            1219 => "Multiple connections to server with different users (đóng kết nối cũ trong File Explorer).",
            1326 => "Logon failure: bad username or password (sai user/mật khẩu hoặc domain).",
            1327 => "User account disabled.",
            1907 => "Mật khẩu đã hết hạn hoặc tài khoản bắt buộc đổi mật khẩu lần đầu. Trên server (máy 172.20.115.40): đổi mật khẩu user backup_user, bỏ chọn 'User must change password at next logon', có thể chọn 'Password never expires'.",
            2242 => "Password expired.",
            _ => $"Windows error code: {errorCode}. Xem https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes"
        };
    }

    /// <summary>
    /// Disconnects from the network share.
    /// </summary>
    public void Disconnect()
    {
        if (_isConnected && !string.IsNullOrEmpty(_networkPath))
        {
            try
            {
                WNetCancelConnection2(_networkPath, 0, false);
            }
            catch
            {
                // Ignore errors during disconnect
            }
            finally
            {
                _isConnected = false;
                _networkPath = string.Empty;
            }
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
