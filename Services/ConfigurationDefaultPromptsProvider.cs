using Microsoft.Extensions.Options;
using PromptProvider.Interfaces;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class ConfigurationDefaultPromptsProvider : IDefaultPromptsProvider
{
    private readonly PromptsOptions _options;
    private readonly PromptKeyOptions _promptKeyOptions;

    public ConfigurationDefaultPromptsProvider(
        IOptions<PromptsOptions> options,
        IOptions<PromptKeyOptions> promptKeyOptions)
    {
        _options = options.Value;
        _promptKeyOptions = promptKeyOptions.Value;
    }

    public IReadOnlyDictionary<string, string> GetDefaults()
    {
        return _options.Defaults;
    }

    public IReadOnlyDictionary<string, PromptConfiguration> GetPromptKeys()
    {
        return _promptKeyOptions.PromptKeys;
    }
}
