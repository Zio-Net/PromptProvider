namespace PromptProvider.Models;

public record ChatPromptResponse
{
    public required string PromptKey { get; set; }
    public required ChatMessage[] ChatMessages { get; set; }
    public int? Version { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Tags { get; set; }
    public PromptKind Type { get; set; } = PromptKind.Chat;
    public LangfusePromptConfiguration? Config { get; set; }
    public PromptSource Source { get; set; }
}
