using PromptProvider.Models;

namespace PromptProvider.Options;

/// <summary>
/// Legacy mapping options: logical prompt key -> Langfuse key/label/version.
/// Prefer <see cref="PromptsOptions.PromptEntries"/> and <see cref="PromptsOptions.Entries"/> for new configurations.
/// </summary>
public class PromptKeyOptions
{
    public Dictionary<string, PromptConfiguration> PromptKeys { get; set; } = new();
}
