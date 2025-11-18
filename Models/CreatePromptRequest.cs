namespace PromptProvider.Models;

public record CreatePromptRequest
{
    public required string PromptKey { get; set; }
    public required string Content { get; set; }
    public string? CommitMessage { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Tags { get; set; }
}
