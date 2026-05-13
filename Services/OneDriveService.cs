using System;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using quanlyfilesBE.DTOs;
namespace quanlyfilesBE.Services;

public interface IOneDriveService
{
    Task<OneDriveExtractResponseDto> ExtractItemAsync(string sourceUrl);
    Task<OneDriveSearchResponseDto> SearchAsync(string sourceUrl, string query, int maxResults);
}

public class OneDriveService : IOneDriveService
{
    private const string DebugEndpointUrl = "http://127.0.0.1:7243/ingest/f756d36b-7fc7-4eca-a996-a24d8a1a5faf";
    private const string DebugSessionId = "938357";
    private const string DebugRunId = "pre-config";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OneDriveService> _logger;

    public OneDriveService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OneDriveService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<OneDriveExtractResponseDto> ExtractItemAsync(string sourceUrl)
    {
        return CallExtractFlowAsync(sourceUrl);
    }

    public Task<OneDriveSearchResponseDto> SearchAsync(string sourceUrl, string query, int maxResults)
    {
        return CallSearchFlowAsync(sourceUrl, query, maxResults);
    }

    private string GetRequiredFlowUrl(string configKey)
    {
        // Ví dụ configKey: "PowerAutomate:OneDriveExtractFlowURL"
        var url = _configuration[configKey];

        var powerAutomateSectionExists = _configuration.GetSection("PowerAutomate").Exists();
        var extractUrl = _configuration["PowerAutomate:OneDriveExtractFlowURL"];
        var searchUrl = _configuration["PowerAutomate:OneDriveSearchFlowURL"];
        var extractLen = extractUrl?.Length ?? -1;
        var searchLen = searchUrl?.Length ?? -1;
        var urlLen = url?.Length ?? -1;
        var isMissing = string.IsNullOrWhiteSpace(url);

        bool isAbsoluteHttpOrHttps = false;
        string? parsedScheme = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(url) &&
                Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                parsedScheme = uri.Scheme;
                isAbsoluteHttpOrHttps = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
            }
        }
        catch
        {
            // ignore parsing errors; debug log will show absolute-http-or-https=false
        }

        #region agent log
        TrySendDebugLog(
            hypothesisId: "H1_missing_onedrive_flow_url_value",
            location: "OneDriveService:GetRequiredFlowUrl",
            message: "Check OneDrive PowerAutomate flow config values (length only)",
            data: new
            {
                configKey = configKey,
                powerAutomateSectionExists,
                requestedUrlLen = urlLen,
                requestedUrlIsMissing = isMissing,
                extractUrlLen = extractLen,
                searchUrlLen = searchLen,
                requestedUrlIsAbsoluteHttpOrHttps = isAbsoluteHttpOrHttps,
                requestedUrlParsedScheme = parsedScheme
            }
        );
        #endregion

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                $"Missing configuration '{configKey}'. Please set Power Automate flow URL for OneDrive integration.");
        return url;
    }

    private void TrySendDebugLog(string hypothesisId, string location, string message, object? data)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var payload = new
            {
                sessionId = DebugSessionId,
                runId = DebugRunId,
                hypothesisId,
                location,
                message,
                data,
                timestamp
            };

            // Send to debug logging server endpoint (it will persist to debug log file).
            var json = JsonSerializer.Serialize(payload);
            var req = new HttpRequestMessage(HttpMethod.Post, DebugEndpointUrl)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("X-Debug-Session-Id", DebugSessionId);

            _httpClient.SendAsync(req).GetAwaiter().GetResult();
        }
        catch
        {
            // Never block the main flow for debug logging failures.
        }
    }

    private async Task<OneDriveExtractResponseDto> CallExtractFlowAsync(string sourceUrl)
    {
        var flowUrl = GetRequiredFlowUrl("PowerAutomate:OneDriveExtractFlowURL");

        var payload = new
        {
            sourceUrl = sourceUrl
        };

        var response = await _httpClient.PostAsJsonAsync(flowUrl, payload);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OneDrive extract flow failed. Status={(int)response.StatusCode}. Content={content}");

        return ParseExtractResponse(content);
    }

    private async Task<OneDriveSearchResponseDto> CallSearchFlowAsync(string sourceUrl, string query, int maxResults)
    {
        var flowUrl = GetRequiredFlowUrl("PowerAutomate:OneDriveSearchFlowURL");

        var payload = new
        {
            sourceUrl = sourceUrl,
            query = query,
            maxResults = maxResults
        };

        var response = await _httpClient.PostAsJsonAsync(flowUrl, payload);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OneDrive search flow failed. Status={(int)response.StatusCode}. Content={content}");

        return ParseSearchResponse(content);
    }

    private OneDriveExtractResponseDto ParseExtractResponse(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Expected: { "item": { ... }, "message": "..." }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("item", out var itemEl))
            {
                var item = itemEl.Deserialize<OneDriveItemDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
                return new OneDriveExtractResponseDto { Item = item, Message = message };
            }

            // Fallback: try deserialize directly
            var res = JsonSerializer.Deserialize<OneDriveExtractResponseDto>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (res != null) return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse OneDrive extract response. Raw content length={Len}", content?.Length ?? 0);
        }

        // If flow returns unexpected payload, still return empty but with message.
        return new OneDriveExtractResponseDto
        {
            Item = null,
            Message = "Unexpected response format from OneDrive extract flow."
        };
    }

    private OneDriveSearchResponseDto ParseSearchResponse(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Expected: { "results": [ { ... } ] , "message": "..." }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("results", out var resultsEl))
            {
                var results = resultsEl.Deserialize<System.Collections.Generic.List<OneDriveItemDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new System.Collections.Generic.List<OneDriveItemDto>();
                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
                return new OneDriveSearchResponseDto { Results = results, Message = message };
            }

            // Fallback: if root is array itself
            if (root.ValueKind == JsonValueKind.Array)
            {
                var results = root.Deserialize<System.Collections.Generic.List<OneDriveItemDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new System.Collections.Generic.List<OneDriveItemDto>();
                return new OneDriveSearchResponseDto { Results = results };
            }

            // Fallback: try deserialize directly
            var res = JsonSerializer.Deserialize<OneDriveSearchResponseDto>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (res != null) return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse OneDrive search response. Raw content length={Len}", content?.Length ?? 0);
        }

        return new OneDriveSearchResponseDto
        {
            Results = new System.Collections.Generic.List<OneDriveItemDto>(),
            Message = "Unexpected response format from OneDrive search flow."
        };
    }
}

