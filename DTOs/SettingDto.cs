namespace quanlyfilesBE.DTOs;

public class SettingDto
{
    public int SettingId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateFileStoragePathDto
{
    public string Path { get; set; } = string.Empty;
}

public class ValidatePathDto
{
    public string Path { get; set; } = string.Empty;
}

public class ValidatePathResponseDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SystemSettingsDto
{
    public string FileStoragePath { get; set; } = string.Empty;
    public string SignatureStoragePath { get; set; } = string.Empty;
    public bool SendEmailNotifications { get; set; } = true; // Default to email notifications
    public int DesignerWarningDays { get; set; } = 2; // Default: warning 2 days before expected finish
    public int ReviewerWarningDays { get; set; } = 1; // Default: warning 1 day before confirmation
    public int SyncIntervalMinutes { get; set; } = 2; // Interval (minutes) for syncing files from client PCs
}

public class UpdateNotificationSettingsDto
{
    public bool SendEmailNotifications { get; set; }
}

public class UpdateWarningDaysSettingsDto
{
    public int DesignerWarningDays { get; set; }
    public int ReviewerWarningDays { get; set; }
}

public class UpdateSyncIntervalDto
{
    public int SyncIntervalMinutes { get; set; }
}