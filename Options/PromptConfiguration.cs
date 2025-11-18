namespace PromptProvider.Options;

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
    /// If neither version nor label is specified, defaults to "production"
    /// </summary>
    public string? Label { get; set; }
}
