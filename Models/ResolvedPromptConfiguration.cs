namespace PromptProvider.Models;

public sealed record ResolvedPromptConfiguration
{
    public required string LogicalKey { get; init; }
    public required string ActualKey { get; init; }
    public string? Label { get; init; }
    public int? Version { get; init; }
    public string? DefaultContent { get; init; }
    public ChatMessage[]? ChatDefaultContent { get; init; }
}
