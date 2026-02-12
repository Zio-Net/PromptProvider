using Microsoft.Extensions.Options;
using PromptProvider.Interfaces;
using PromptProvider.Models;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class ConfigurationDefaultPromptsProvider(
    IOptions<PromptsOptions> options,
    IOptions<PromptKeyOptions> promptKeyOptions) : IDefaultPromptsProvider
{
    private readonly PromptsOptions _options = options.Value;
    private readonly PromptKeyOptions _promptKeyOptions = promptKeyOptions.Value;
    private readonly IReadOnlyDictionary<string, ResolvedPromptConfiguration> _resolvedPrompts = BuildResolvedPrompts(options.Value, promptKeyOptions.Value);

    public IReadOnlyDictionary<string, string> GetDefaults() => _options.Defaults;

    public IReadOnlyDictionary<string, ChatMessage[]> GetChatDefaults() => _options.ChatDefaults;

    public IReadOnlyDictionary<string, PromptConfiguration> GetPromptKeys() => _promptKeyOptions.PromptKeys;

    public IReadOnlyDictionary<string, ResolvedPromptConfiguration> GetResolvedPrompts() => _resolvedPrompts;

    public bool TryGetResolvedPrompt(string logicalKey, out ResolvedPromptConfiguration configuration)
        => _resolvedPrompts.TryGetValue(logicalKey, out configuration!);

    private static IReadOnlyDictionary<string, ResolvedPromptConfiguration> BuildResolvedPrompts(
        PromptsOptions promptsOptions,
        PromptKeyOptions promptKeyOptions)
    {
        var aggregate = new Dictionary<string, MutablePromptConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var (logicalKey, content) in promptsOptions.Defaults)
        {
            Upsert(aggregate, logicalKey).DefaultContent = content;
        }

        foreach (var (logicalKey, chatContent) in promptsOptions.ChatDefaults)
        {
            Upsert(aggregate, logicalKey).ChatDefaultContent = chatContent;
        }

        foreach (var (logicalKey, promptConfig) in promptKeyOptions.PromptKeys)
        {
            var item = Upsert(aggregate, logicalKey);
            if (!string.IsNullOrWhiteSpace(promptConfig.Key)) item.ActualKey = promptConfig.Key;
            if (!string.IsNullOrWhiteSpace(promptConfig.Label)) item.Label = promptConfig.Label;
            if (promptConfig.Version.HasValue) item.Version = promptConfig.Version;
        }

        foreach (var (logicalKey, entry) in promptsOptions.PromptEntries)
        {
            ApplyUnifiedEntry(aggregate, logicalKey, entry);
        }

        foreach (var entry in promptsOptions.Entries)
        {
            var logicalKey = !string.IsNullOrWhiteSpace(entry.Name)
                ? entry.Name
                : entry.Key;

            if (string.IsNullOrWhiteSpace(logicalKey))
            {
                continue;
            }

            ApplyUnifiedEntry(aggregate, logicalKey, entry);
        }

        return aggregate.ToDictionary(
            pair => pair.Key,
            pair => new ResolvedPromptConfiguration
            {
                LogicalKey = pair.Key,
                ActualKey = string.IsNullOrWhiteSpace(pair.Value.ActualKey) ? pair.Key : pair.Value.ActualKey!,
                Label = pair.Value.Label,
                Version = pair.Value.Version,
                DefaultContent = pair.Value.DefaultContent,
                ChatDefaultContent = pair.Value.ChatDefaultContent
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyUnifiedEntry(
        IDictionary<string, MutablePromptConfig> aggregate,
        string logicalKey,
        PromptEntryOptions entry)
    {
        var item = Upsert(aggregate, logicalKey);
        if (!string.IsNullOrWhiteSpace(entry.Key)) item.ActualKey = entry.Key;
        if (!string.IsNullOrWhiteSpace(entry.Label)) item.Label = entry.Label;
        if (entry.Version.HasValue) item.Version = entry.Version;
        if (!string.IsNullOrWhiteSpace(entry.Default)) item.DefaultContent = entry.Default;
        if (entry.ChatDefault is not null && entry.ChatDefault.Length > 0) item.ChatDefaultContent = entry.ChatDefault;
    }

    private static MutablePromptConfig Upsert(IDictionary<string, MutablePromptConfig> aggregate, string logicalKey)
    {
        if (!aggregate.TryGetValue(logicalKey, out var item))
        {
            item = new MutablePromptConfig();
            aggregate[logicalKey] = item;
        }

        return item;
    }

    private sealed class MutablePromptConfig
    {
        public string? ActualKey { get; set; }
        public string? Label { get; set; }
        public int? Version { get; set; }
        public string? DefaultContent { get; set; }
        public ChatMessage[]? ChatDefaultContent { get; set; }
    }
}
