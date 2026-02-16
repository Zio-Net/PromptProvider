using PromptProvider.Models;

namespace PromptProvider.Options;

/// <summary>
/// Prompt configuration options.
/// Supports both legacy split configuration and unified entries.
/// </summary>
public class PromptsOptions
{
    /// <summary>
    /// Legacy text defaults map: logical key -> default prompt.
    /// </summary>
    public Dictionary<string, string> Defaults { get; set; } = [];

    /// <summary>
    /// Legacy chat defaults map: logical key -> default chat messages.
    /// </summary>
    public Dictionary<string, ChatMessage[]> ChatDefaults { get; set; } = [];

    /// <summary>
    /// Unified prompt entries keyed by logical prompt key.
    /// </summary>
    public Dictionary<string, PromptEntryOptions> PromptEntries { get; set; } = [];

    /// <summary>
    /// Unified prompt entries as array.
    /// If Name is omitted, Key is used as the logical key.
    /// </summary>
    public List<PromptEntryOptions> Entries { get; set; } = [];
}

public sealed class PromptEntryOptions
{
    /// <summary>
    /// Logical key used by SDK callers.
    /// If omitted in array form, Key is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Langfuse prompt key/name.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Optional default label used when caller does not provide label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Optional default version used when caller does not provide version.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Optional text fallback value.
    /// </summary>
    public string? Default { get; set; }

    /// <summary>
    /// Optional chat fallback value.
    /// </summary>
    public ChatMessage[]? ChatDefault { get; set; }
}
