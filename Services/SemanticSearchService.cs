using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Services;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly HttpClient _http;
    private readonly SemanticSearchOptions _options;
    private readonly GemmaReasoningOptions _gemmaReasoningOptions;
    private readonly ILogger<SemanticSearchService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private const string DebugEndpoint = "http://127.0.0.1:7253/ingest/f756d36b-7fc7-4eca-a996-a24d8a1a5faf";
    private const string DebugSessionId = "da37a0";

    public SemanticSearchService(
        HttpClient http,
        IOptions<SemanticSearchOptions> options,
        IOptions<GemmaReasoningOptions> gemmaReasoningOptions,
        ILogger<SemanticSearchService> logger)
    {
        _http = http;
        _options = options.Value;
        _gemmaReasoningOptions = gemmaReasoningOptions.Value;
        _logger = logger;
    }

    public async Task<SemanticSearchResponse> SearchAsync(string folderPath, string query, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.QdrantUrl))
        {
            return new SemanticSearchResponse
            {
                Error = "Semantic search chưa cấu hình SemanticSearch:QdrantUrl."
            };
        }

        if (UseOpenClaw())
        {
            if (string.IsNullOrWhiteSpace(_options.OpenClawUrl))
            {
                return new SemanticSearchResponse
                {
                    Error = "EmbedProvider=OpenClaw nhưng chưa cấu hình SemanticSearch:OpenClawUrl (vd http://localhost:3000)."
                };
            }
        }
        else if (string.IsNullOrWhiteSpace(_options.OllamaUrl))
        {
            return new SemanticSearchResponse
            {
                Error = "Semantic search chưa cấu hình SemanticSearch:OllamaUrl, hoặc đặt EmbedProvider=OpenClaw nếu chỉ có Ollama trong Docker + gateway."
            };
        }

        var folderPrefix = folderPath.Trim().Replace('/', '\\').TrimEnd('\\');
        var q = query.Trim();
        if (string.IsNullOrEmpty(folderPrefix) || string.IsNullOrEmpty(q))
        {
            return new SemanticSearchResponse { Results = Array.Empty<SemanticSearchResultDto>() };
        }

        var searchLimit = Math.Max(1, _options.SearchLimit);
        var finalLimit = Math.Min(Math.Max(1, maxResults), Math.Max(1, _options.ResultLimit));

        float[] vector;
        try
        {
            vector = await GetEmbeddingAsync(q, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var label = UseOpenClaw() ? "OpenClaw" : "Ollama";
            _logger.LogWarning(ex, "{Provider} embedding failed", label);
            var hint = UseOpenClaw()
                ? ""
                : " Neu Ollama chay trong Docker ma khong publish 11434: hay dat EmbedProvider=OpenClaw va OpenClawUrl (gateway, vd http://localhost:3000) trong appsettings.";
            return new SemanticSearchResponse { Error = $"Embedding ({label}) lỗi: {ex.Message}.{hint}" };
        }

        List<QdrantHit> rawHits;
        try
        {
            rawHits = await QdrantSearchAsync(vector, searchLimit, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Qdrant search failed");
            return new SemanticSearchResponse { Error = $"Qdrant search lỗi: {ex.Message}" };
        }

        var strongTokens = BuildStrongTokens(q);
        if (strongTokens.Count > 0)
        {
            var strictSourceHits = await FindStrictSourceMatchesAsync(strongTokens, searchLimit, cancellationToken).ConfigureAwait(false);
            if (strictSourceHits.Count > 0)
            {
                // Nếu query chứa mã TBKT cụ thể và tìm được source khớp cứng trong DB,
                // ưu tiên tập này thay vì topK semantic rộng.
                rawHits = strictSourceHits;
            }
        }

        var keywordTokens = BuildKeywordTokens(q);
        var candidatesCount = rawHits.Count;

        var hitsAfterKeyword = ApplyHybridKeywordFilter(rawHits, keywordTokens);
        var hitsAfterStrongToken = ApplyStrongTokenFilter(hitsAfterKeyword, strongTokens);

        var rootForCombine = !string.IsNullOrWhiteSpace(_options.IndexRoot)
            ? _options.IndexRoot!.Trim().Replace('/', '\\').TrimEnd('\\')
            : folderPrefix;

        var resolved = new List<(QdrantHit Hit, string FullPath)>();
        foreach (var hit in hitsAfterStrongToken)
        {
            var source = hit.Source ?? "";
            var full = CombinePaths(rootForCombine, source);
            if (!FullPathMatchesUserFolder(folderPrefix, full))
                continue;
            resolved.Add((hit, full));
        }

        var deduped = DedupeByPath(resolved, strongTokens);
        deduped.Sort((a, b) =>
        {
            var byBoost = b.SourceBoost.CompareTo(a.SourceBoost);
            if (byBoost != 0) return byBoost;
            return b.Score.CompareTo(a.Score);
        });

        var results = deduped
            .Take(finalLimit)
            .Select(x => new SemanticSearchResultDto
            {
                Name = Path.GetFileName(x.FullPath.TrimEnd('\\')),
                Path = x.FullPath,
                FullPath = x.FullPath,
                Score = x.Score,
                Snippet = BuildAnswerSnippet(x.Text, keywordTokens, strongTokens)
            })
            .ToList();

        var aiAnswer = await TryGenerateAiAnswerAsync(q, results, cancellationToken).ConfigureAwait(false);

        return new SemanticSearchResponse
        {
            Results = results,
            CandidatesCount = candidatesCount,
            AiAnswer = aiAnswer
        };
    }

    private bool UseOpenClaw() =>
        string.Equals((_options.EmbedProvider ?? "Ollama").Trim(), "OpenClaw", StringComparison.OrdinalIgnoreCase);

    private async Task<float[]> GetEmbeddingAsync(string prompt, CancellationToken ct)
    {
        if (UseOpenClaw())
            return await GetEmbeddingOpenClawAsync(prompt, ct).ConfigureAwait(false);
        return await GetEmbeddingOllamaDirectAsync(prompt, ct).ConfigureAwait(false);
    }

    private async Task<float[]> GetEmbeddingOllamaDirectAsync(string prompt, CancellationToken ct)
    {
        var baseUrl = _options.OllamaUrl.TrimEnd('/');
        var url = $"{baseUrl}/api/embeddings";
        var body = new { model = _options.EmbedModel, prompt };
        using var resp = await _http.PostAsJsonAsync(url, body, JsonOpts, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("embedding", out var emb))
            throw new InvalidOperationException("Ollama response thieu embedding.");
        return ParseEmbeddingArray(emb);
    }

    /// <summary>OpenAI-compatible: POST /v1/embeddings + header x-openclaw-model (openclaw docs).</summary>
    private async Task<float[]> GetEmbeddingOpenClawAsync(string prompt, CancellationToken ct)
    {
        var baseUrl = _options.OpenClawUrl!.TrimEnd('/');
        var url = $"{baseUrl}/v1/embeddings";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = JsonContent.Create(new { model = "openclaw", input = prompt }, options: JsonOpts);
        var modelHeader = string.IsNullOrWhiteSpace(_options.OpenClawModelHeader)
            ? $"ollama/{_options.EmbedModel}"
            : _options.OpenClawModelHeader.Trim();
        req.Headers.TryAddWithoutValidation("x-openclaw-model", modelHeader);
        if (!string.IsNullOrWhiteSpace(_options.OpenClawGatewayToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenClawGatewayToken.Trim());
        else if (!string.IsNullOrWhiteSpace(_options.OpenClawBasicUsername))
        {
            var plain = $"{_options.OpenClawBasicUsername}:{_options.OpenClawBasicPassword ?? ""}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if ((int)resp.StatusCode == 401)
            throw new InvalidOperationException("OpenClaw gateway tra ve 401 Unauthorized. Kiem tra Basic Auth (OpenClawBasicUsername/OpenClawBasicPassword) hoac bearer token.");
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            throw new InvalidOperationException("OpenClaw/OpenAI response thieu data[0].embedding.");
        var first = data[0];
        if (!first.TryGetProperty("embedding", out var emb))
            throw new InvalidOperationException("OpenClaw response thieu embedding.");
        return ParseEmbeddingArray(emb);
    }

    private static float[] ParseEmbeddingArray(JsonElement emb)
    {
        var list = new List<float>();
        foreach (var x in emb.EnumerateArray())
            list.Add((float)x.GetDouble());
        if (list.Count == 0)
            throw new InvalidOperationException("Vector embedding rong.");
        return list.ToArray();
    }

    private async Task<List<QdrantHit>> QdrantSearchAsync(float[] vector, int limit, CancellationToken ct)
    {
        var baseUrl = _options.QdrantUrl.TrimEnd('/');
        var col = Uri.EscapeDataString(_options.Collection);
        var url = $"{baseUrl}/collections/{col}/points/search";
        var body = new
        {
            vector,
            limit,
            with_payload = true,
            with_vector = false
        };
        using var resp = await _http.PostAsJsonAsync(url, body, JsonOpts, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("result", out var arr))
            return new List<QdrantHit>();

        var hits = new List<QdrantHit>();
        foreach (var el in arr.EnumerateArray())
        {
            var score = el.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0;
            string? source = null;
            string? text = null;
            if (el.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String)
                    source = s.GetString();
                if (payload.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    text = t.GetString();
            }
            hits.Add(new QdrantHit(score, source, text));
        }
        return hits;
    }

    /// <summary>
    /// Quét collection để lấy các source khớp cứng mã có số (vd 24050f + 2000kva).
    /// Dùng cho truy vấn định danh mã TBKT để tránh semantic topK bỏ sót.
    /// </summary>
    private async Task<List<QdrantHit>> FindStrictSourceMatchesAsync(List<string> strongTokens, int limit, CancellationToken ct)
    {
        if (strongTokens.Count == 0)
            return new List<QdrantHit>();

        var baseUrl = _options.QdrantUrl.TrimEnd('/');
        var col = Uri.EscapeDataString(_options.Collection);
        var url = $"{baseUrl}/collections/{col}/points/scroll";

        object? offset = null;
        var found = new List<QdrantHit>();

        for (var i = 0; i < 128; i++)
        {
            var body = new Dictionary<string, object?>
            {
                ["limit"] = 256,
                ["with_payload"] = true,
                ["with_vector"] = false
            };
            if (offset != null)
                body["offset"] = offset;

            using var resp = await _http.PostAsJsonAsync(url, body, JsonOpts, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("result", out var result))
                break;
            if (!result.TryGetProperty("points", out var points) || points.ValueKind != JsonValueKind.Array || points.GetArrayLength() == 0)
                break;

            foreach (var el in points.EnumerateArray())
            {
                string? source = null;
                string? text = null;
                if (el.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
                {
                    if (payload.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String)
                        source = s.GetString();
                    if (payload.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                        text = t.GetString();
                }

                var sourceNorm = NormalizeAlphaNum(source ?? "");
                if (strongTokens.All(t => sourceNorm.Contains(t, StringComparison.Ordinal)))
                {
                    var priority = ComputeStrictSourcePriority(source ?? "");
                    found.Add(new QdrantHit(priority, source, text));
                }
            }

            if (!result.TryGetProperty("next_page_offset", out var next) || next.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                break;

            offset = JsonSerializer.Deserialize<object>(next.GetRawText());
        }

        return found
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    private static int ComputeStrictSourcePriority(string source)
    {
        var file = Path.GetFileName(source).ToLowerInvariant();
        var score = 0;
        if (file.StartsWith("tbkt ", StringComparison.Ordinal) || file.StartsWith("tbkt_", StringComparison.Ordinal))
            score += 1000;
        if (file.Contains("(kho)", StringComparison.Ordinal))
            score += 100;
        if (file.Contains("-a4", StringComparison.Ordinal))
            score -= 10;
        return score;
    }

    private static List<string> BuildKeywordTokens(string query)
    {
        return query
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .ToList();
    }

    /// <summary>
    /// Token "mạnh" thường là mã hồ sơ/tbkt có số (vd: 24050f, 2000kva).
    /// Khi có token này thì ưu tiên lọc theo source để tránh trả nhiều file lan man.
    /// </summary>
    private static List<string> BuildStrongTokens(string query)
    {
        return Regex.Matches(query.ToLowerInvariant(), "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(v => v.Length >= 4 && v.Any(char.IsDigit) && v.Any(char.IsLetter))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<QdrantHit> ApplyHybridKeywordFilter(List<QdrantHit> hits, List<string> tokens)
    {
        if (tokens.Count == 0)
            return hits;
        var filtered = hits.Where(h =>
        {
            var blob = $"{h.Text ?? ""} {h.Source ?? ""}".ToLowerInvariant();
            return tokens.TrueForAll(t => blob.Contains(t, StringComparison.Ordinal));
        }).ToList();
        return filtered.Count > 0 ? filtered : hits;
    }

    private static List<QdrantHit> ApplyStrongTokenFilter(List<QdrantHit> hits, List<string> strongTokens)
    {
        if (strongTokens.Count == 0)
            return hits;

        var ranked = hits
            .Select(h =>
            {
                var sourceNorm = NormalizeAlphaNum(h.Source ?? "");
                var matchCount = strongTokens.Count(t => sourceNorm.Contains(t, StringComparison.Ordinal));
                return new { Hit = h, MatchCount = matchCount };
            })
            .Where(x => x.MatchCount > 0)
            .OrderByDescending(x => x.MatchCount)
            .ThenByDescending(x => x.Hit.Score)
            .Select(x => x.Hit)
            .ToList();

        // Nếu không match được mã nào trong source, fallback về semantic gốc để tránh rỗng.
        return ranked.Count > 0 ? ranked : hits;
    }

    private static string NormalizeAlphaNum(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", "");
    }

    private static string CombinePaths(string root, string source)
    {
        root = root.Trim().Replace('/', '\\').TrimEnd('\\');
        var s = source.Trim().Replace('/', '\\').TrimStart('\\');
        if (string.IsNullOrEmpty(s))
            return root;
        if (IsAbsolutePhysicalPath(s))
            return s;
        return Path.Combine(root, s);
    }

    private static bool IsAbsolutePhysicalPath(string s)
    {
        s = s.Replace('/', '\\');
        if (s.StartsWith("\\\\", StringComparison.Ordinal))
            return true;
        return s.Length >= 3 && char.IsLetter(s[0]) && s[1] == ':' && (s[2] == '\\' || s[2] == '/');
    }

    /// <summary>Tinh thần giống <see cref="Controllers.FilesController.SearchFiles"/>: prefix trực tiếp hoặc khớp đuôi đường dẫn từ ổ (M:\… → …\relative… trên UNC).</summary>
    private static bool FullPathMatchesUserFolder(string folderPrefix, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(folderPrefix) || string.IsNullOrWhiteSpace(fullPath))
            return false;
        var fp = folderPrefix.Trim().Replace('/', '\\').TrimEnd('\\');
        var p = fullPath.Trim().Replace('/', '\\');
        if (p.StartsWith(fp + "\\", StringComparison.OrdinalIgnoreCase))
            return true;
        if (p.Equals(fp, StringComparison.OrdinalIgnoreCase))
            return true;

        string? relativePath = null;
        if (fp.Length >= 2 && fp[1] == ':' && (fp.Length == 2 || fp[2] == '\\') && fp.Length > 3)
            relativePath = fp.Substring(3).TrimStart('\\');

        if (!string.IsNullOrEmpty(relativePath))
        {
            if (p.EndsWith("\\" + relativePath, StringComparison.OrdinalIgnoreCase))
                return true;
            if (p.Equals(relativePath, StringComparison.OrdinalIgnoreCase))
                return true;
            var withSep = "\\" + relativePath + "\\";
            if (p.Contains(withSep, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static List<(string FullPath, double Score, string? Text, int SourceBoost)> DedupeByPath(
        List<(QdrantHit Hit, string FullPath)> items,
        List<string> strongTokens)
    {
        var best = new Dictionary<string, (double Score, string? Text, int SourceBoost)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hit, full) in items)
        {
            var key = full.TrimEnd('\\');
            var boost = ComputeSourceBoost(hit.Source ?? "", strongTokens);
            if (!best.TryGetValue(key, out var cur) || boost > cur.SourceBoost || (boost == cur.SourceBoost && hit.Score > cur.Score))
                best[key] = (hit.Score, hit.Text, boost);
        }
        return best.Select(kv => (kv.Key, kv.Value.Score, kv.Value.Text, kv.Value.SourceBoost)).ToList();
    }

    /// <summary>
    /// Boost source theo mức độ khớp mã:
    /// - +100: source chứa chuỗi mã ghép liền (vd 24050f2000kva)
    /// - +10 mỗi token mạnh có mặt trong source
    /// </summary>
    private static int ComputeSourceBoost(string source, List<string> strongTokens)
    {
        if (strongTokens.Count == 0)
            return 0;

        var sourceNorm = NormalizeAlphaNum(source);
        var boost = 0;

        var merged = string.Concat(strongTokens);
        if (!string.IsNullOrEmpty(merged) && sourceNorm.Contains(merged, StringComparison.Ordinal))
            boost += 100;

        foreach (var token in strongTokens)
        {
            if (sourceNorm.Contains(token, StringComparison.Ordinal))
                boost += 10;
        }

        return boost;
    }

    /// <summary>
    /// Trích đoạn gần nhất với câu hỏi thay vì luôn lấy phần đầu tài liệu.
    /// Ưu tiên câu chứa nhiều token query (đặc biệt token mã TBKT).
    /// </summary>
    private static string? BuildAnswerSnippet(string? text, List<string> keywordTokens, List<string> strongTokens)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        var allTokens = keywordTokens
            .Concat(strongTokens)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length >= 3)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sentences = Regex.Split(normalized, @"(?<=[\.\!\?;:])\s+")
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (sentences.Count == 0)
            sentences.Add(normalized);

        string best = sentences[0];
        var bestScore = int.MinValue;
        foreach (var s in sentences)
        {
            var score = ScoreSentenceForTokens(s, allTokens, strongTokens);
            if (score > bestScore)
            {
                bestScore = score;
                best = s;
            }
        }

        const int max = 320;
        if (best.Length <= max)
            return best;
        return best[..max] + "…";
    }

    private static int ScoreSentenceForTokens(string sentence, List<string> allTokens, List<string> strongTokens)
    {
        var lower = sentence.ToLowerInvariant();
        var normalizedAlpha = NormalizeAlphaNum(sentence);
        var score = 0;

        foreach (var t in allTokens)
        {
            if (lower.Contains(t, StringComparison.Ordinal))
                score += 3;
            if (normalizedAlpha.Contains(t, StringComparison.Ordinal))
                score += 2;
        }

        foreach (var t in strongTokens)
        {
            if (lower.Contains(t, StringComparison.Ordinal) || normalizedAlpha.Contains(t, StringComparison.Ordinal))
                score += 10;
        }

        // Phạt nhẹ câu quá ngắn/không có ngữ nghĩa
        if (sentence.Length < 20) score -= 2;
        return score;
    }

    private sealed record QdrantHit(double Score, string? Source, string? Text);

    private async Task<string?> TryGenerateAiAnswerAsync(
        string query,
        IReadOnlyList<SemanticSearchResultDto> results,
        CancellationToken ct)
    {
        if (!_options.GenerateAiAnswer)
            return null;
        var useOpenClawResponses = !string.IsNullOrWhiteSpace(_options.OpenClawUrl);
        if (useOpenClawResponses && string.IsNullOrWhiteSpace(_options.OpenClawUrl))
            return null;
        if (!useOpenClawResponses && string.IsNullOrWhiteSpace(_options.OllamaUrl))
            return null;

        if (results.Count == 0)
            return "Khong tim thay tai lieu phu hop tu du lieu da index.";

        var maxFiles = Math.Max(1, _options.ChatMaxFilesInPrompt);
        var top = results.Take(maxFiles).ToList();
        // #region agent log
        DebugLog("H6", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:entry", "Contextual answer generation started", new
        {
            useOpenClaw = useOpenClawResponses,
            resultsCount = results.Count,
            topCount = top.Count,
            firstName = top.FirstOrDefault()?.Name,
            firstSnippetPrefix = top.FirstOrDefault()?.Snippet?.Substring(0, Math.Min(120, top.FirstOrDefault()?.Snippet?.Length ?? 0))
        }, "run-before-fix");
        // #endregion
        var prompt = BuildGemmaPrompt(query, top);

        try
        {
            if (useOpenClawResponses)
            {
                var text = await CallOpenClawResponsesAsync(prompt, ct).ConfigureAwait(false);
                // #region agent log
                DebugLog("H7", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:openclaw", "OpenClaw response received for contextual answer", new
                {
                    textLength = text?.Length ?? 0,
                    textPreview = text?.Substring(0, Math.Min(220, text?.Length ?? 0))
                }, "run-before-fix");
                // #endregion
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
                var localAnswer = BuildLocalSnippetAnswer(query, top);
                // #region agent log
                DebugLog("H13", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:local-fallback-empty-openclaw", "OpenClaw empty response, fallback to local snippet summary", new
                {
                    localAnswerPreview = localAnswer.Substring(0, Math.Min(220, localAnswer.Length))
                }, "run-before-fix");
                // #endregion
                return localAnswer;
            }
            else
            {
                var baseUrl = _options.OllamaUrl.TrimEnd('/');
                var url = $"{baseUrl}/api/generate";
                var body = new
                {
                    model = string.IsNullOrWhiteSpace(_options.ChatModel) ? "gemma3:4b" : _options.ChatModel.Trim(),
                    prompt,
                    stream = false
                };
                using var resp = await _http.PostAsJsonAsync(url, body, JsonOpts, ct).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                if (doc.RootElement.TryGetProperty("response", out var answer) && answer.ValueKind == JsonValueKind.String)
                {
                    var text = answer.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
                var localAnswer = BuildLocalSnippetAnswer(query, top);
                // #region agent log
                DebugLog("H13", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:local-fallback-empty-ollama", "Ollama empty response, fallback to local snippet summary", new
                {
                    localAnswerPreview = localAnswer.Substring(0, Math.Min(220, localAnswer.Length))
                }, "run-before-fix");
                // #endregion
                return localAnswer;
            }
        }
        catch (Exception ex)
        {
            // #region agent log
            DebugLog("H8", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:catch", "Contextual answer generation failed", new
            {
                error = ex.Message,
                provider = useOpenClawResponses ? "openclaw" : "ollama"
            }, "run-before-fix");
            // #endregion
            _logger.LogWarning(ex, "Contextual answer generation failed");
            var localAnswer = BuildLocalSnippetAnswer(query, top, ex.Message);
            // #region agent log
            DebugLog("H13", "SemanticSearchService.cs:TryGenerateAiAnswerAsync:local-fallback-catch", "LLM call failed, fallback to local snippet summary", new
            {
                localAnswerPreview = localAnswer.Substring(0, Math.Min(220, localAnswer.Length)),
                llmError = ex.Message
            }, "run-before-fix");
            // #endregion
            return localAnswer;
        }

    }

    private static string BuildGemmaPrompt(string query, IReadOnlyList<SemanticSearchResultDto> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ban la tro ly tra cuu tai lieu noi bo.");
        sb.AppendLine("Hay tra loi bang tieng Viet, uu tien thong tin ky thuat tu trich doan ben duoi, khong bịa them thong tin.");
        sb.AppendLine("Neu cau hoi dang hoi thong so/noi dung chi tiet, phai tom tat thong tin cu the (thong so, gia tri, dac tinh) neu co trong trich doan.");
        sb.AppendLine("Neu khong du thong tin chi tiet, noi ro 'chua thay du lieu chi tiet trong trich doan' va neu ten file de user mo xem.");
        sb.AppendLine();
        sb.AppendLine($"Cau hoi: {query}");
        sb.AppendLine("Danh sach ket qua tim kiem:");

        for (var i = 0; i < results.Count; i++)
        {
            var item = results[i];
            sb.AppendLine($"{i + 1}. Ten file: {item.Name}");
            sb.AppendLine($"   Duong dan: {item.FullPath}");
            if (!string.IsNullOrWhiteSpace(item.Snippet))
                sb.AppendLine($"   Trich doan: {item.Snippet}");
            if (item.Score.HasValue)
                sb.AppendLine($"   Do tuong dong: {item.Score.Value:F4}");
        }

        sb.AppendLine();
        sb.AppendLine("Yeu cau tra loi:");
        sb.AppendLine("- Tra loi bang tieng Viet.");
        sb.AppendLine("- Uu tien noi dung ky thuat co that trong trich doan, co the dung bullet ngan gon.");
        sb.AppendLine("- Neu co file phu hop nhat, neu ro ten file va duong dan.");
        sb.AppendLine("- Toi da 6 cau.");
        return sb.ToString();
    }

    public async Task<ReasoningAnswerResponse> GenerateReasoningAnswerAsync(
        string query,
        string? folderPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!_gemmaReasoningOptions.Enabled)
        {
            return new ReasoningAnswerResponse
            {
                Error = "Che do suy luan da bi tat trong cau hinh he thong."
            };
        }
        if (string.IsNullOrWhiteSpace(_options.OpenClawUrl))
        {
            return new ReasoningAnswerResponse
            {
                Error = "Chua cau hinh SemanticSearch:OpenClawUrl de goi OpenClaw Responses API."
            };
        }

        var prompt = BuildReasoningPrompt(query, folderPath);

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _gemmaReasoningOptions.TimeoutSeconds)));
            var text = await CallOpenClawResponsesAsync(prompt, linkedCts.Token).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text))
                return new ReasoningAnswerResponse { Answer = text };

            return new ReasoningAnswerResponse
            {
                Error = "OpenClaw khong tra ve noi dung tra loi hop le."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw reasoning failed");
            return new ReasoningAnswerResponse
            {
                Error = $"Khong the sinh cau tra loi suy luan luc nay: {ex.Message}"
            };
        }
    }

    public async Task<IntentDetectionResponse> DetectIntentAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new IntentDetectionResponse { Intent = "file_search", Confidence = 0.5 };
        }
        if (!_gemmaReasoningOptions.Enabled)
        {
            return new IntentDetectionResponse { Intent = "file_search", Confidence = 0.0, Error = "Gemma intent detection is disabled." };
        }
        if (string.IsNullOrWhiteSpace(_options.OpenClawUrl))
        {
            return new IntentDetectionResponse { Intent = "file_search", Confidence = 0.0, Error = "OpenClawUrl is missing." };
        }

        var prompt = BuildIntentDetectionPrompt(query);

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _gemmaReasoningOptions.TimeoutSeconds)));
            var text = await CallOpenClawResponsesAsync(prompt, linkedCts.Token).ConfigureAwait(false);
            return ParseIntentDetection(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw intent detection failed");
            return new IntentDetectionResponse
            {
                Intent = "file_search",
                Confidence = 0.0,
                Error = ex.Message
            };
        }
    }

    private static string BuildReasoningPrompt(string query, string? folderPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ban la tro ly AI noi bo. Tra loi bang tieng Viet, ngan gon, chinh xac.");
        sb.AppendLine("Muc tieu: giai thich, suy luan va tinh toan dua tren cau hoi nguoi dung.");
        sb.AppendLine("Neu cau hoi thieu du lieu de tinh toan chinh xac, neu ro gia dinh can bo sung.");
        sb.AppendLine("Khong bịa so lieu noi bo hoac duong dan file.");
        sb.AppendLine("Neu nguoi dung thuc chat dang muon tim file, hay goi y nhap ma TBKT/GDN de tim bang.");
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            sb.AppendLine($"Ngu canh thu muc hien tai: {folderPath.Trim()}");
        }
        sb.AppendLine();
        sb.AppendLine($"Cau hoi: {query.Trim()}");
        sb.AppendLine();
        sb.AppendLine("Yeu cau dinh dang:");
        sb.AppendLine("- Toi da 6 cau.");
        sb.AppendLine("- Neu can tinh toan, trinh bay tung buoc ngan gon.");
        return sb.ToString();
    }

    private static string BuildIntentDetectionPrompt(string query)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Phan loai y dinh truy van thanh 1 trong 2 gia tri:");
        sb.AppendLine("- file_search: nguoi dung muon tim file, duong dan, tai lieu.");
        sb.AppendLine("- reasoning: nguoi dung hoi noi dung, thong so, giai thich, tong hop trong file.");
        sb.AppendLine("Neu co ma file/TBKT va dong thoi hoi noi dung ('thong so', 'la gi', 'noi dung', 'chi tiet') thi uu tien reasoning.");
        sb.AppendLine("Tra ve DUY NHAT JSON 1 dong theo schema:");
        sb.AppendLine("{\"intent\":\"file_search|reasoning\",\"confidence\":0.0}");
        sb.AppendLine("Khong them giai thich.");
        sb.AppendLine($"Query: {query.Trim()}");
        return sb.ToString();
    }

    private static IntentDetectionResponse ParseIntentDetection(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new IntentDetectionResponse { Intent = "file_search", Confidence = 0.0, Error = "Empty response" };

        var jsonCandidate = raw.Trim();
        var start = jsonCandidate.IndexOf('{');
        var end = jsonCandidate.LastIndexOf('}');
        if (start >= 0 && end > start)
            jsonCandidate = jsonCandidate.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(jsonCandidate);
            var intent = doc.RootElement.TryGetProperty("intent", out var intentEl) && intentEl.ValueKind == JsonValueKind.String
                ? intentEl.GetString()?.Trim().ToLowerInvariant()
                : null;
            var confidence = doc.RootElement.TryGetProperty("confidence", out var confEl) && confEl.ValueKind is JsonValueKind.Number
                ? confEl.GetDouble()
                : 0.0;
            var normalizedIntent = intent is "reasoning" ? "reasoning" : "file_search";
            var clamped = Math.Max(0.0, Math.Min(1.0, confidence));
            return new IntentDetectionResponse
            {
                Intent = normalizedIntent,
                Confidence = clamped
            };
        }
        catch
        {
            return new IntentDetectionResponse
            {
                Intent = "file_search",
                Confidence = 0.0,
                Error = $"Invalid JSON intent payload: {raw}"
            };
        }
    }

    private async Task<string> CallOpenClawResponsesAsync(string inputPrompt, CancellationToken ct)
    {
        var baseUrl = _options.OpenClawUrl!.TrimEnd('/');
        var url = $"{baseUrl}/v1/responses";
        var body = new
        {
            model = string.IsNullOrWhiteSpace(_options.OpenClawResponsesModel) ? "agent:main" : _options.OpenClawResponsesModel.Trim(),
            input = inputPrompt,
            stream = false
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = JsonContent.Create(body, options: JsonOpts);
        AddOpenClawAuth(req);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var contentPreview = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            // #region agent log
            DebugLog("H10", "SemanticSearchService.cs:CallOpenClawResponsesAsync:http-error", "OpenClaw /v1/responses returned non-success", new
            {
                url,
                status = (int)resp.StatusCode,
                reason = resp.ReasonPhrase,
                bodyPreview = contentPreview.Substring(0, Math.Min(260, contentPreview.Length))
            }, "run-before-fix");
            // #endregion
            resp.EnsureSuccessStatusCode();
        }
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return ExtractOpenClawOutputText(doc.RootElement);
    }

    private static string BuildLocalSnippetAnswer(string query, IReadOnlyList<SemanticSearchResultDto> top, string? llmError = null)
    {
        var first = top.FirstOrDefault();
        if (first == null)
            return "Chua tim thay du lieu chi tiet de tong hop thong so ky thuat.";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(first.Snippet))
            parts.Add(first.Snippet!.Trim());
        var second = top.Skip(1).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Snippet));
        if (second != null)
            parts.Add(second.Snippet!.Trim());

        if (parts.Count == 0)
        {
            return $"Chua thay du lieu chi tiet trong trich doan. Nguon tham chieu: {first.Name}.";
        }

        var raw = string.Join(" ", parts).Replace('\n', ' ').Replace('\r', ' ');
        var normalized = Regex.Replace(raw, @"\s+", " ").Trim();
        var lower = normalized.ToLowerInvariant();

        var kva = Regex.Match(lower, @"\b(\d{2,5})\s*kva\b", RegexOptions.IgnoreCase);
        var voltage = Regex.Match(lower, @"\b(\d{1,3}(?:\+\d+x\d+(?:\.\d+)?)?%?\s*\/\s*\d+(?:\.\d+)?\s*kv)\b", RegexOptions.IgnoreCase);
        var vector = Regex.Match(normalized, @"\bDyn\d+\b", RegexOptions.IgnoreCase);
        var phase = Regex.Match(normalized, @"\b(3Ø|3Pha|3 Pha)\b", RegexOptions.IgnoreCase);

        var clauses = new List<string>();
        if (kva.Success) clauses.Add($"Cong suat dinh muc: {kva.Groups[1].Value} kVA");
        if (voltage.Success) clauses.Add($"Dien ap: {voltage.Groups[1].Value.Replace(" ", "")}");
        if (vector.Success) clauses.Add($"To dau day: {vector.Value}");
        if (phase.Success) clauses.Add($"So pha: {phase.Value}");

        if (clauses.Count == 0)
        {
            var fallbackClause = Regex.Split(normalized, @"(?<=[\.\;\:\,\|])\s+")
                .Select(x => Regex.Replace(x, @"\s+", " ").Trim())
                .Where(x => x.Length >= 15 && x.Length <= 120)
                .Where(x => Regex.IsMatch(x, @"\d"))
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fallbackClause))
                clauses.Add(fallbackClause);
        }

        if (clauses.Count == 0)
        {
            var merged = normalized.Length > 220 ? normalized.Substring(0, 220).Trim() + "..." : normalized;
            clauses.Add(merged);
        }

        var header = !string.IsNullOrWhiteSpace(llmError)
            ? "OpenClaw AI chua phan hoi duoc, tam tom tat tu OCR:"
            : "Tom tat thong so/chi tiet tim thay:";
        // #region agent log
        DebugLog("H14", "SemanticSearchService.cs:BuildLocalSnippetAnswer:structured-extract", "Structured OCR extraction for fallback answer", new
        {
            llmError = llmError ?? "",
            clausesCount = clauses.Count,
            clausesPreview = clauses.Take(4).ToArray()
        }, "run-before-fix");
        // #endregion
        var bullet = string.Join("\n", clauses.Select(c => $"- {c}"));
        return $"{header}\n{bullet}";
    }

    private static string ExtractOpenClawOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var topText) && topText.ValueKind == JsonValueKind.String)
            return topText.GetString()?.Trim() ?? string.Empty;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in content.EnumerateArray())
                {
                    if (c.ValueKind != JsonValueKind.Object)
                        continue;
                    if (c.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        var t = textEl.GetString();
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.Append(t.Trim());
                        }
                    }
                }
            }
            else if (item.TryGetProperty("text", out var plainText) && plainText.ValueKind == JsonValueKind.String)
            {
                var t = plainText.GetString();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(t.Trim());
                }
            }
        }
        return sb.ToString().Trim();
    }

    private void AddOpenClawAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrWhiteSpace(_options.OpenClawGatewayToken))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenClawGatewayToken.Trim());
            return;
        }
        if (!string.IsNullOrWhiteSpace(_options.OpenClawBasicUsername))
        {
            var plain = $"{_options.OpenClawBasicUsername}:{_options.OpenClawBasicPassword ?? ""}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
    }

    private static void DebugLog(string hypothesisId, string location, string message, object data, string runId)
    {
        // #region agent log
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, DebugEndpoint);
                req.Headers.TryAddWithoutValidation("X-Debug-Session-Id", DebugSessionId);
                req.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        sessionId = DebugSessionId,
                        runId,
                        hypothesisId,
                        location,
                        message,
                        data,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }),
                    Encoding.UTF8,
                    "application/json");
                await client.SendAsync(req).ConfigureAwait(false);
            }
            catch
            {
                // ignore debug logging errors
            }
        });
        // #endregion
    }
}
