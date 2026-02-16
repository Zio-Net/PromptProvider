namespace PromptProvider.Interfaces;

using PromptProvider.Models;

public interface IDefaultPromptsProvider
{
    IReadOnlyDictionary<string, string> GetDefaults();

    IReadOnlyDictionary<string, ChatMessage[]> GetChatDefaults();

    IReadOnlyDictionary<string, PromptConfiguration> GetPromptKeys();

    IReadOnlyDictionary<string, ResolvedPromptConfiguration> GetResolvedPrompts();

    bool TryGetResolvedPrompt(string logicalKey, out ResolvedPromptConfiguration configuration);
}
