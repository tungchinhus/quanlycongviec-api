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

