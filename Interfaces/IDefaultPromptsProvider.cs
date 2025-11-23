namespace PromptProvider.Interfaces;

using PromptProvider.Options;

public interface IDefaultPromptsProvider
{
    IReadOnlyDictionary<string, string> GetDefaults();

    IReadOnlyDictionary<string, PromptConfiguration> GetPromptKeys();
}
