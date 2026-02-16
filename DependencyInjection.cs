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
        Action<PromptsOptions>? configurePrompts = null,
        Action<PromptKeyOptions>? configurePromptKeys = null)
    {
        var langfuseBuilder = services.AddOptions<LangfuseOptions>();
        if (configureLangfuse is not null)
        {
            langfuseBuilder.Configure(configureLangfuse);
        }

        langfuseBuilder
            .Validate(IsLangfuseConfigValid,
                "Langfuse configuration is invalid. Provide all of BaseUrl/PublicKey/SecretKey, and ensure BaseUrl is an absolute URI, or leave all blank.")
            .ValidateOnStart();

        if (configurePrompts is not null) services.Configure(configurePrompts);
        if (configurePromptKeys is not null) services.Configure(configurePromptKeys);

        services.AddHttpClient<ILangfuseService, LangfuseService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LangfuseOptions>>().Value;
            if (options.IsConfigured() && !string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            if (options.HttpClient.RequestTimeoutSeconds is > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(options.HttpClient.RequestTimeoutSeconds.Value);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LangfuseOptions>>().Value;
            return new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                MaxConnectionsPerServer = options.HttpClient.MaxConnectionsPerServer.GetValueOrDefault(int.MaxValue),
                PooledConnectionLifetime = options.HttpClient.PooledConnectionLifetimeMinutes is > 0
                    ? TimeSpan.FromMinutes(options.HttpClient.PooledConnectionLifetimeMinutes.Value)
                    : Timeout.InfiniteTimeSpan
            };
        });

        services.AddScoped<IPromptService, PromptService>();
        services.AddSingleton<IDefaultPromptsProvider, ConfigurationDefaultPromptsProvider>();

        return services;
    }

    private static bool IsLangfuseConfigValid(LangfuseOptions options)
    {
        var hasAny = !string.IsNullOrWhiteSpace(options.BaseUrl)
                     || !string.IsNullOrWhiteSpace(options.PublicKey)
                     || !string.IsNullOrWhiteSpace(options.SecretKey);

        if (!hasAny)
        {
            return true;
        }

        if (!options.IsConfigured())
        {
            return false;
        }

        return Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _);
    }
}
