namespace quanlyfilesBE.Models;

public class SemanticSearchOptions
{
    public const string SectionName = "SemanticSearch";

    /// <summary>Gốc index trên đĩa (tùy chọn). Nếu để trống, ghép <c>source</c> với <c>folderPath</c> từ request.</summary>
    public string? IndexRoot { get; set; }

    public string QdrantUrl { get; set; } = "http://localhost:6333";
    public string Collection { get; set; } = "data_mba_ptk";

    /// <summary>Ollama = POST truc tiep len OllamaUrl. OpenClaw = POST len OpenClawUrl/v1/embeddings (khi Ollama chi chay trong Docker, gateway thuong mo port 3000).</summary>
    public string EmbedProvider { get; set; } = "Ollama";

    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string EmbedModel { get; set; } = "nomic-embed-text";
    public string ChatModel { get; set; } = "gemma3:4b";
    public bool GenerateAiAnswer { get; set; } = true;
    public int ChatMaxFilesInPrompt { get; set; } = 5;

    /// <summary>Base URL gateway OpenClaw (vd http://localhost:3000). Chi dung khi EmbedProvider = OpenClaw.</summary>
    public string? OpenClawUrl { get; set; }

    public string? OpenClawGatewayToken { get; set; }

    /// <summary>Tuỳ chọn: Basic Auth username nếu OpenClaw proxy yêu cầu xác thực (vd Caddy basic_auth).</summary>
    public string? OpenClawBasicUsername { get; set; }

    /// <summary>Tuỳ chọn: Basic Auth password nếu OpenClaw proxy yêu cầu xác thực.</summary>
    public string? OpenClawBasicPassword { get; set; }

    /// <summary>Gia tri header x-openclaw-model (vd ollama/nomic-embed-text).</summary>
    public string? OpenClawModelHeader { get; set; }
    /// <summary>Model/agent id cho OpenClaw /v1/responses (vd agent:main).</summary>
    public string OpenClawResponsesModel { get; set; } = "agent:main";

    /// <summary>Số điểm lấy từ Qdrant trước khi lọc folder / keyword / dedupe.</summary>
    public int SearchLimit { get; set; } = 40;

    /// <summary>Số file tối đa trả về sau dedupe (mặc định nếu client không truyền maxResults).</summary>
    public int ResultLimit { get; set; } = 10;

    public int HttpTimeoutSeconds { get; set; } = 120;
}
