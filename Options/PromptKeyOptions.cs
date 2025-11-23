namespace PromptProvider.Options;

public class PromptKeyOptions
{
    /// <summary>
    /// A mapping of logical prompt names to a PromptConfiguration which contains the Langfuse key and optional default label.
    /// Example in appsettings.json:
    /// "PromptKeys": {
    ///   "ChatTitle": { "Key": "chat.title.generate", "Label": "production" },
    ///   "SystemDefault": { "Key": "prompts.system.default", "Label": "production" }
    /// }
    /// </summary>
    public Dictionary<string, PromptConfiguration> PromptKeys { get; set; } = new();
}
