using System;

namespace quanlyfilesBE.DTOs;

public class OneDriveItemDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    // Đường dẫn (tùy flow có thể trả relative hoặc full).
    public string? Path { get; set; }
    public string? FullPath { get; set; }

    // Link view file/folder trên web (nếu flow trả).
    public string? WebUrl { get; set; }

    public long? Size { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
}

public class OneDriveExtractRequestDto
{
    // Có thể là URL OneDrive/SharePoint (folder hoặc file).
    public string SourceUrl { get; set; } = string.Empty;
}

public class OneDriveSearchRequestDto
{
    public string SourceUrl { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 100;
}

public class OneDriveExtractResponseDto
{
    public OneDriveItemDto? Item { get; set; }
    public string? Message { get; set; }
}

public class OneDriveSearchResponseDto
{
    public System.Collections.Generic.List<OneDriveItemDto> Results { get; set; } = new();
    public string? Message { get; set; }
}

