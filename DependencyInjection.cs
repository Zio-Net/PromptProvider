using Microsoft.Extensions.DependencyInjection;
using PromptProvider.Interfaces;
using PromptProvider.Options;
using PromptProvider.Services;

namespace PromptProvider;

public static class DependencyInjection
{
    public static IServiceCollection AddPromptProvider(
        this IServiceCollection services,
        Action<LangfuseOptions>? configureLangfuse = null,
        Action<PromptsOptions>? configurePrompts = null)
    {
        if (configureLangfuse is not null) services.Configure(configureLangfuse);
        if (configurePrompts is not null) services.Configure(configurePrompts);

        services.AddHttpClient<ILangfuseService, LangfuseService>();
        services.AddScoped<IPromptService, PromptService>();

        // Register default prompts provider (users may replace with custom implementation)
        services.AddSingleton<IDefaultPromptsProvider, ConfigurationDefaultPromptsProvider>();

        return services;
    }
}
