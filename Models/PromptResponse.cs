namespace PromptProvider.Models;

public record PromptResponse
{
    public required string PromptKey { get; set; }
    public required string Content { get; set; }
    public int? Version { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Tags { get; set; }
    public string? Type { get; set; }
    public LangfusePromptConfiguration? Config { get; set; }
    public string? Source { get; set; } // "Langfuse" or "Local"
}
