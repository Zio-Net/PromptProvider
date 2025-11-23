namespace PromptProvider.Options;

public sealed class PromptConfiguration
{
    public required string Key { get; set; }
    public int? Version { get; set; }
    public string? Label { get; set; }
}
