# SyncToServer - Windows Worker Service

A .NET 8 Windows Worker Service that automatically backs up files from a local OneDrive folder to a network server.

## Features

- ✅ **Automatic OneDrive Detection**: Automatically finds OneDrive folder in user profile
- ✅ **Secure Network Authentication**: Uses Windows P/Invoke (`WNetAddConnection2`) for secure SMB share access
- ✅ **Smart File Copy**: Only copies new or modified files (compares `LastWriteTime`)
- ✅ **Retry Mechanism**: Automatically retries locked files (important for OneDrive sync)
- ✅ **Comprehensive Logging**: Uses Serilog with console and file sinks
- ✅ **Configurable**: Easy configuration via `appsettings.json`

## Configuration

Edit `appsettings.json`:

```json
{
  "SyncSettings": {
    "SourcePath": "",  // Leave empty to auto-detect OneDrive
    "DestServer": "\\\\ServerIP\\BackupData",
    "NetworkCredentials": {
      "Username": "backup_user",
      "Password": "your_password_here",
      "Domain": ""  // Optional, leave empty if not using domain
    },
    "IntervalMinutes": 15
  }
}
```

### Configuration Fields

- **SourcePath**: Leave empty to auto-detect OneDrive, or specify a custom path
- **DestServer**: UNC path to the network share (e.g., `\\192.168.1.100\BackupData`)
- **NetworkCredentials.Username**: Username for network authentication
- **NetworkCredentials.Password**: Password for network authentication
- **NetworkCredentials.Domain**: Domain name (optional, leave empty if not using domain)
- **IntervalMinutes**: How often to sync (in minutes)

## Building the Project

```bash
cd syncToServer
dotnet build
```

## Running as Console Application (Development)

```bash
dotnet run
```

## Publishing as Single Executable

To create a single `.exe` file for deployment:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The executable will be in: `bin\Release\net8.0\win-x64\publish\syncToServer.exe`

## Installing as Windows Service

1. **Build and publish** the application (see above)

2. **Install the service** using `sc` command (run as Administrator):
   ```powershell
   sc create SyncToServer binPath="C:\Path\To\syncToServer.exe" start=auto
   ```

3. **Start the service**:
   ```powershell
   sc start SyncToServer
   ```

4. **View logs**: Check the `logs` folder in the application directory

## How It Works

1. **Network Authentication**: Uses `WNetAddConnection2` (P/Invoke) to securely connect to the SMB share before copying files. This is the recommended Microsoft approach for Windows Services.

2. **Smart Copy Logic**:
   - Recursively scans the source directory
   - For each file, checks if destination exists
   - Only copies if file is new OR source `LastWriteTime` is newer than destination
   - Skips files that are already up-to-date

3. **Retry Mechanism**:
   - If a file is locked (IOException), retries up to 3 times
   - Uses exponential backoff (1s, 2s, 4s delays)
   - Important for OneDrive files that may be locked during sync

4. **Periodic Execution**:
   - Uses `PeriodicTimer` for efficient scheduled execution
   - Performs initial sync immediately on startup
   - Then syncs at configured intervals

## Logs

Logs are written to:
- **Console**: Real-time output
- **File**: `logs\sync-YYYYMMDD.log` (daily rolling, 30 days retention)

## Troubleshooting

### Service cannot connect to network share

- Verify the network path is accessible from the machine
- Check username/password are correct
- Ensure the service account has network access permissions
- Check Windows Firewall settings

### Files not copying

- Check logs for specific error messages
- Verify source path exists and is accessible
- Ensure destination share has write permissions
- Check if files are locked by other processes

### OneDrive path not detected

- Manually set `SourcePath` in `appsettings.json`
- Ensure OneDrive is installed and synced
- Check that the OneDrive folder exists in user profile

## Security Notes

- **Password Storage**: Consider using User Secrets for development or Windows Credential Manager for production
- **Service Account**: The service runs under the account that installed it. Ensure this account has appropriate permissions
- **Network Credentials**: Never commit passwords to source control. Use environment variables or secure configuration

## License

This project is provided as-is for internal use.
