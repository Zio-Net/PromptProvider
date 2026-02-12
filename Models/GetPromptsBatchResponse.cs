namespace PromptProvider.Models;

public sealed record GetPromptsBatchResponse
{
    public required IReadOnlyList<PromptResponse> Prompts { get; init; }
    public required IReadOnlyList<string> NotFound { get; init; }
}
