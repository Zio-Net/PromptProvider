namespace PromptProvider.Models;

public sealed class PromptConfiguration
{
    /// <summary>
    /// The Langfuse prompt key/name.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Optional default version used when method argument version is not provided.
    /// If effective version exists, effective label is ignored.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Optional default label used when method argument label is not provided.
    /// Ignored when effective version exists.
    /// </summary>
    public string? Label { get; set; }
}
