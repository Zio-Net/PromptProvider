namespace PromptProvider.Models;

public sealed class PromptConfiguration
{
    /// <summary>
    /// The prompt key/name
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Optional specific version number to use
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Optional label to use (e.g., "production", "staging", "latest")
    /// When configured, this label will be automatically used when fetching the prompt,
    /// unless explicitly overridden by passing a different label to GetPromptAsync/GetChatPromptAsync.
    /// If neither version nor label is specified, Langfuse will use its default behavior.
    /// </summary>
    public string? Label { get; set; }
}
