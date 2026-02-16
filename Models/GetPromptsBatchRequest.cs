namespace PromptProvider.Models;

public sealed record GetPromptsBatchRequest
{
    public required IReadOnlyList<PromptConfiguration> Prompts { get; init; }
}
