using Microsoft.Extensions.Options;
using PromptProvider.Interfaces;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class ConfigurationDefaultPromptsProvider : IDefaultPromptsProvider
{
    private readonly PromptsOptions _options;

    public ConfigurationDefaultPromptsProvider(IOptions<PromptsOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyDictionary<string, string> GetDefaults()
    {
        return _options.Defaults;
    }
}
