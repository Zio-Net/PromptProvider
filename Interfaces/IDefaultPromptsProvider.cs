namespace PromptProvider.Interfaces;

public interface IDefaultPromptsProvider
{
    IReadOnlyDictionary<string, string> GetDefaults();
}
