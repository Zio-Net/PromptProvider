using Microsoft.Extensions.Logging;
using PromptProvider.Interfaces;
using PromptProvider.Models;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class PromptService(
    ILogger<PromptService> logger,
    ILangfuseService langfuseService,
    IDefaultPromptsProvider defaultPromptsProvider,
    Microsoft.Extensions.Options.IOptions<LangfuseOptions> langfuseOptions) : IPromptService
{
    private readonly ILogger<PromptService> _logger = logger;
    private readonly ILangfuseService _langfuseService = langfuseService;
    private readonly IDefaultPromptsProvider _defaultPromptsProvider = defaultPromptsProvider;
    private readonly Microsoft.Extensions.Options.IOptions<LangfuseOptions> _langfuseOptions = langfuseOptions;

    public async Task<PromptResponse> CreatePromptAsync(CreatePromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequired(request.PromptKey, nameof(request), "PromptKey is required.");
        ValidateRequired(request.Content, nameof(request), "Content is required.");

        EnsureLangfuseConfigured(request.PromptKey, "create prompt");

        try
        {
            var resolved = ResolvePromptRequest(request.PromptKey, null, null);
            _logger.LogInformation("Creating prompt for PromptKey '{PromptKey}' with ActualKey '{ActualKey}'", request.PromptKey, resolved.actualKey);

            var langfuseRequest = new CreateLangfusePromptRequest
            {
                Name = resolved.actualKey,
                Prompt = request.Content,
                Type = "text",
                CommitMessage = request.CommitMessage,
                Labels = request.Labels ?? [],
                Tags = request.Tags ?? []
            };

            var created = await _langfuseService.CreatePromptAsync(langfuseRequest, cancellationToken);
            return new PromptResponse
            {
                PromptKey = created.Name,
                Content = created.Prompt,
                Version = created.Version,
                Labels = created.Labels,
                Tags = created.Tags,
                Type = ParsePromptKind(created.Type),
                Config = created.Config as LangfusePromptConfiguration,
                Source = PromptSource.Langfuse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt for PromptKey '{PromptKey}'", request.PromptKey);
            throw;
        }
    }

    public async Task<ChatPromptResponse> CreateChatPromptAsync(CreateChatPromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequired(request.PromptKey, nameof(request), "PromptKey is required.");
        if (request.ChatMessages == null || request.ChatMessages.Length == 0)
        {
            throw new ArgumentException("ChatMessages are required.", nameof(request));
        }

        EnsureLangfuseConfigured(request.PromptKey, "create chat prompt");

        try
        {
            var resolved = ResolvePromptRequest(request.PromptKey, null, null);
            _logger.LogInformation("Creating chat prompt for PromptKey '{PromptKey}' with ActualKey '{ActualKey}'", request.PromptKey, resolved.actualKey);

            var langfuseRequest = new CreateLangfuseChatPromptRequest
            {
                Name = resolved.actualKey,
                Prompt = request.ChatMessages,
                Type = "chat",
                CommitMessage = request.CommitMessage,
                Labels = request.Labels ?? [],
                Tags = request.Tags ?? []
            };

            var created = await _langfuseService.CreateChatPromptAsync(langfuseRequest, cancellationToken);
            return new ChatPromptResponse
            {
                PromptKey = created.Name,
                ChatMessages = created.Prompt,
                Version = created.Version,
                Labels = created.Labels,
                Tags = created.Tags,
                Type = ParsePromptKind(created.Type),
                Config = created.Config as LangfusePromptConfiguration,
                Source = PromptSource.Langfuse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat prompt for PromptKey '{PromptKey}'", request.PromptKey);
            throw;
        }
    }

    public async Task<PromptResponse?> GetPromptAsync(string promptKey, int? version = null, string? label = null, CancellationToken cancellationToken = default)
    {
        ValidateRequired(promptKey, nameof(promptKey), "PromptKey is required.");
        var resolved = ResolvePromptRequest(promptKey, version, label);

        if (_langfuseOptions.Value.IsConfigured())
        {
            try
            {
                _logger.LogInformation(PromptLoggingEvents.LangfuseFetchPrompt,
                    "Fetching prompt with PromptKey '{PromptKey}', ActualKey '{ActualKey}', Version '{Version}', Label '{Label}'",
                    promptKey, resolved.actualKey, resolved.version, resolved.label);

                var langfusePrompt = await _langfuseService.GetPromptAsync(resolved.actualKey, resolved.version, resolved.label, cancellationToken);
                if (langfusePrompt != null)
                {
                    return new PromptResponse
                    {
                        PromptKey = langfusePrompt.Name,
                        Content = langfusePrompt.Prompt,
                        Version = langfusePrompt.Version,
                        Labels = langfusePrompt.Labels,
                        Tags = langfusePrompt.Tags,
                        Type = ParsePromptKind(langfusePrompt.Type),
                        Config = langfusePrompt.Config,
                        Source = PromptSource.Langfuse
                    };
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(PromptLoggingEvents.LangfuseFallback, ex,
                    "Failed remote prompt fetch for PromptKey '{PromptKey}', ActualKey '{ActualKey}'. Falling back to local defaults.",
                    promptKey, resolved.actualKey);
            }
        }

        return GetPromptFromDefaults(promptKey);
    }

    public async Task<ChatPromptResponse?> GetChatPromptAsync(string promptKey, int? version = null, string? label = null, CancellationToken cancellationToken = default)
    {
        ValidateRequired(promptKey, nameof(promptKey), "PromptKey is required.");
        var resolved = ResolvePromptRequest(promptKey, version, label);

        if (_langfuseOptions.Value.IsConfigured())
        {
            try
            {
                _logger.LogInformation(PromptLoggingEvents.LangfuseFetchChatPrompt,
                    "Fetching chat prompt with PromptKey '{PromptKey}', ActualKey '{ActualKey}', Version '{Version}', Label '{Label}'",
                    promptKey, resolved.actualKey, resolved.version, resolved.label);

                var langfusePrompt = await _langfuseService.GetChatPromptAsync(resolved.actualKey, resolved.version, resolved.label, cancellationToken);
                if (langfusePrompt != null)
                {
                    return new ChatPromptResponse
                    {
                        PromptKey = langfusePrompt.Name,
                        ChatMessages = langfusePrompt.Prompt,
                        Version = langfusePrompt.Version,
                        Labels = langfusePrompt.Labels,
                        Tags = langfusePrompt.Tags,
                        Type = ParsePromptKind(langfusePrompt.Type),
                        Config = langfusePrompt.Config,
                        Source = PromptSource.Langfuse
                    };
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(PromptLoggingEvents.LangfuseFallback, ex,
                    "Failed remote chat prompt fetch for PromptKey '{PromptKey}', ActualKey '{ActualKey}'. Falling back to local defaults.",
                    promptKey, resolved.actualKey);
            }
        }

        return GetChatPromptFromDefaults(promptKey);
    }

    public async Task<IReadOnlyList<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default)
    {
        EnsureLangfuseConfigured("*", "get all prompts");
        return await _langfuseService.GetAllPromptsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PromptResponse>> GetPromptsAsync(
        IEnumerable<string> promptKeys,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptKeys);

        var keys = promptKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (keys.Length == 0)
        {
            throw new ArgumentException("At least one prompt key is required.", nameof(promptKeys));
        }

        // Determine concurrency limit: honor MaxConnectionsPerServer if configured, otherwise default to 10
        var maxConcurrency = _langfuseOptions.Value.HttpClient.MaxConnectionsPerServer ?? 10;
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = keys.Select(async key =>
        {
            var acquired = false;
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                acquired = true;
                return await GetPromptAsync(key, label: label, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(PromptLoggingEvents.BatchPromptItemFailed, ex,
                    "Batch fetch failed for PromptKey '{PromptKey}'. Continuing with other keys.", key);
                return null;
            }
            finally
            {
                if (acquired)
                {
                    semaphore.Release();
                }
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).Select(r => r!).ToArray();
    }

    public async Task<PromptResponse> UpdatePromptLabelsAsync(
        string promptKey,
        int version,
        UpdatePromptLabelsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequired(promptKey, nameof(promptKey), "PromptKey is required.");
        ArgumentNullException.ThrowIfNull(request);

        EnsureLangfuseConfigured(promptKey, "update prompt labels");

        var resolved = ResolvePromptRequest(promptKey, version, null);
        var updated = await _langfuseService.UpdatePromptLabelsAsync(resolved.actualKey, resolved.version ?? version, request, cancellationToken);

        if (!string.Equals(updated.Type, "text", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected text prompt but received {updated.Type} prompt");
        }

        return new PromptResponse
        {
            PromptKey = updated.Name,
            Content = updated.Prompt,
            Version = updated.Version,
            Labels = updated.Labels,
            Tags = updated.Tags,
            Type = ParsePromptKind(updated.Type),
            Config = updated.Config,
            Source = PromptSource.Langfuse
        };
    }

    private (string actualKey, int? version, string? label) ResolvePromptRequest(string logicalKey, int? providedVersion, string? providedLabel)
    {
        if (!_defaultPromptsProvider.TryGetResolvedPrompt(logicalKey, out var resolved))
        {
            var fallbackLabel = providedVersion.HasValue ? null : providedLabel;
            return (logicalKey, providedVersion, fallbackLabel);
        }

        int? effectiveVersion;
        string? effectiveLabel;

        if (providedVersion.HasValue)
        {
            // Explicit version takes precedence over any configured defaults or labels.
            effectiveVersion = providedVersion;
            effectiveLabel = null;
        }
        else if (!string.IsNullOrEmpty(providedLabel))
        {
            // Explicit label prevents applying a configured default version.
            effectiveVersion = null;
            effectiveLabel = providedLabel;
        }
        else
        {
            // No explicit version or label: fall back to configured defaults.
            effectiveVersion = resolved.Version;
            effectiveLabel = effectiveVersion.HasValue ? null : resolved.Label;
        }
        return (resolved.ActualKey, effectiveVersion, effectiveLabel);
    }

    private PromptResponse? GetPromptFromDefaults(string promptKey)
    {
        if (_defaultPromptsProvider.TryGetResolvedPrompt(promptKey, out var resolved) && !string.IsNullOrWhiteSpace(resolved.DefaultContent))
        {
            _logger.LogInformation(PromptLoggingEvents.LocalDefaultReturned,
                "Returning local text default for PromptKey '{PromptKey}'", promptKey);
            return new PromptResponse
            {
                PromptKey = promptKey,
                Content = resolved.DefaultContent,
                Type = PromptKind.Text,
                Source = PromptSource.Local
            };
        }

        return null;
    }

    private ChatPromptResponse? GetChatPromptFromDefaults(string promptKey)
    {
        if (_defaultPromptsProvider.TryGetResolvedPrompt(promptKey, out var resolved) && resolved.ChatDefaultContent is { Length: > 0 })
        {
            _logger.LogInformation(PromptLoggingEvents.LocalDefaultReturned,
                "Returning local chat default for PromptKey '{PromptKey}'", promptKey);
            return new ChatPromptResponse
            {
                PromptKey = promptKey,
                ChatMessages = resolved.ChatDefaultContent,
                Type = PromptKind.Chat,
                Source = PromptSource.Local
            };
        }

        return null;
    }

    private void EnsureLangfuseConfigured(string promptKey, string operation)
    {
        if (_langfuseOptions.Value.IsConfigured())
        {
            return;
        }

        _logger.LogWarning("Cannot {Operation} for PromptKey '{PromptKey}' because Langfuse is not configured", operation, promptKey);
        throw new InvalidOperationException("Langfuse is not configured.");
    }

    private static PromptKind ParsePromptKind(string? type)
    {
        if (string.Equals(type, "text", StringComparison.OrdinalIgnoreCase)) return PromptKind.Text;
        if (string.Equals(type, "chat", StringComparison.OrdinalIgnoreCase)) return PromptKind.Chat;
        return PromptKind.Unknown;
    }

    private static void ValidateRequired(string? value, string argumentName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, argumentName);
        }
    }
}
