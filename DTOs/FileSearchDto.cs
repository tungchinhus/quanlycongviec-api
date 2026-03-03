using System.Collections.Generic;

namespace quanlyfilesBE.DTOs;

public class FileSearchResultDto
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? FullPath { get; set; }
}

public class FileSearchResponseDto
{
    public List<FileSearchResultDto> Results { get; set; } = new();
}

