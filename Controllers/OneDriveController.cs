using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Services;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OneDriveController : ControllerBase
{
    private readonly IOneDriveService _oneDriveService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OneDriveController> _logger;

    public OneDriveController(
        IOneDriveService oneDriveService,
        IConfiguration configuration,
        ILogger<OneDriveController> logger)
    {
        _oneDriveService = oneDriveService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("extract")]
    public async Task<IActionResult> Extract([FromBody] OneDriveExtractRequestDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required" });

            var sourceUrl = dto.SourceUrl;
            if (string.IsNullOrWhiteSpace(sourceUrl))
                sourceUrl = _configuration["PowerAutomate:OneDriveFixedSourceUrl"];

            if (string.IsNullOrWhiteSpace(sourceUrl))
                return BadRequest(new { error = "SourceUrl is required (or set PowerAutomate:OneDriveFixedSourceUrl)" });

            var res = await _oneDriveService.ExtractItemAsync(sourceUrl);
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting OneDrive item");
            return StatusCode(500, new { error = "Failed to extract OneDrive item", message = ex.Message });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] OneDriveSearchRequestDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { error = "Request body is required" });

            var sourceUrl = dto.SourceUrl;
            if (string.IsNullOrWhiteSpace(sourceUrl))
                sourceUrl = _configuration["PowerAutomate:OneDriveFixedSourceUrl"];

            if (string.IsNullOrWhiteSpace(sourceUrl))
                return BadRequest(new { error = "SourceUrl is required (or set PowerAutomate:OneDriveFixedSourceUrl)" });
            if (string.IsNullOrWhiteSpace(dto.Query))
                return BadRequest(new { error = "Query is required" });

            var res = await _oneDriveService.SearchAsync(sourceUrl, dto.Query, dto.MaxResults);
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching OneDrive items");
            return StatusCode(500, new { error = "Failed to search OneDrive items", message = ex.Message });
        }
    }
}

