namespace quanlyfilesBE.Models;

public sealed class GemmaReasoningOptions
{
    public const string SectionName = "GemmaReasoning";

    public bool Enabled { get; set; } = true;
    public int MaxOutputTokens { get; set; } = 512;
    public double Temperature { get; set; } = 0.2;
    public int TimeoutSeconds { get; set; } = 45;
}
