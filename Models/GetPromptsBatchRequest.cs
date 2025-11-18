namespace PromptProvider.Models;

public sealed record GetPromptsBatchRequest
{
    public required List<PromptConfiguration> Prompts { get; init; }
}
