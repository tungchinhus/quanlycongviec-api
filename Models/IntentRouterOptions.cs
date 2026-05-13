namespace quanlyfilesBE.Models;

public sealed class IntentRouterOptions
{
    public const string SectionName = "IntentRouter";

    public bool Enabled { get; set; } = true;
    public bool UseGemmaIntentDetection { get; set; } = true;
    public double GemmaMinConfidence { get; set; } = 0.55;
    public int ReasoningBias { get; set; } = 0;
    public int FileSearchBias { get; set; } = 0;
    public string[] FileSearchKeywords { get; set; } = Array.Empty<string>();
    public string[] ReasoningKeywords { get; set; } = Array.Empty<string>();
}
