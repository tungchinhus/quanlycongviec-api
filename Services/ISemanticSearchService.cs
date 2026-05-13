namespace quanlyfilesBE.Services;

public sealed class SemanticSearchResultDto
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string FullPath { get; init; } = "";
    public double? Score { get; init; }
    public string? Snippet { get; init; }
}

public sealed class SemanticSearchResponse
{
    public IReadOnlyList<SemanticSearchResultDto> Results { get; init; } = Array.Empty<SemanticSearchResultDto>();
    public int CandidatesCount { get; init; }
    public string? AiAnswer { get; init; }
    public string? Error { get; init; }
}

public sealed class ReasoningAnswerResponse
{
    public string? Answer { get; init; }
    public string? Error { get; init; }
}

public sealed class IntentDetectionResponse
{
    public string Intent { get; init; } = "file_search";
    public double Confidence { get; init; }
    public string? Error { get; init; }
}

public interface ISemanticSearchService
{
    Task<SemanticSearchResponse> SearchAsync(string folderPath, string query, int maxResults, CancellationToken cancellationToken = default);
    Task<ReasoningAnswerResponse> GenerateReasoningAnswerAsync(string query, string? folderPath = null, CancellationToken cancellationToken = default);
    Task<IntentDetectionResponse> DetectIntentAsync(string query, CancellationToken cancellationToken = default);
}
