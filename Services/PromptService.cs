using PromptProvider.Models;
using PromptProvider.Interfaces;
using Microsoft.Extensions.Logging;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class PromptService : IPromptService
{
    private readonly ILogger<PromptService> _logger;
    private readonly ILangfuseService _langfuseService;
    private readonly IDefaultPromptsProvider _defaultPromptsProvider;
    private readonly Microsoft.Extensions.Options.IOptions<LangfuseOptions> _langfuseOptions;

    public PromptService(
        ILogger<PromptService> logger,
        ILangfuseService langfuseService,
        IDefaultPromptsProvider defaultPromptsProvider,
        Microsoft.Extensions.Options.IOptions<LangfuseOptions> langfuseOptions)
    {
        _logger = logger;
        _langfuseService = langfuseService;
        _defaultPromptsProvider = defaultPromptsProvider;
        _langfuseOptions = langfuseOptions;
    }

    public async Task<PromptResponse> CreatePromptAsync(CreatePromptRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PromptKey))
        {
            throw new ArgumentException("PromptKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content is required.", nameof(request));
        }

        if (!_langfuseOptions.Value.IsConfigured())
        {
            _logger.LogWarning("Cannot create prompt '{PromptKey}' - Langfuse is not configured", request.PromptKey);
            throw new InvalidOperationException("Langfuse is not configured. Cannot create prompts.");
        }

        try
        {
            // Allow passing a logical key that maps to actual Langfuse key
            var promptKeys = _defaultPromptsProvider.GetPromptKeys();
            var actualKey = promptKeys != null && promptKeys.TryGetValue(request.PromptKey, out var mapped) ? mapped : request.PromptKey;

            _logger.LogInformation("Creating prompt '{PromptKey}' (actual: {ActualKey}) in Langfuse", request.PromptKey, actualKey);

            var langfuseRequest = new CreateLangfusePromptRequest
            {
                Name = actualKey,
                Prompt = request.Content,
                Type = "text",
                CommitMessage = request.CommitMessage,
                Labels = request.Labels ?? Array.Empty<string>(),
                Tags = request.Tags ?? Array.Empty<string>()
            };

            var created = await _langfuseService.CreatePromptAsync(langfuseRequest, cancellationToken);

            return new PromptResponse
            {
                PromptKey = created.Name,
                Content = created.Prompt,
                Version = created.Version,
                Labels = created.Labels,
                Tags = created.Tags,
                Type = created.Type,
                Config = created.Config as LangfusePromptConfiguration,
                Source = "Langfuse"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt '{PromptKey}' in Langfuse", request.PromptKey);
            throw;
        }
    }

    public async Task<PromptResponse?> GetPromptAsync(
        string promptKey,
        int? version = null,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptKey))
        {
            throw new ArgumentException("PromptKey is required.", nameof(promptKey));
        }

        var logicalKey = promptKey; // keep original for local fallback

        // If the promptKey matches a configured logical key, use its mapping (string -> actual langfuse key)
        var promptKeys = _defaultPromptsProvider.GetPromptKeys();
        var actualKey = promptKeys != null && promptKeys.TryGetValue(logicalKey, out var mappedKey) ? mappedKey : logicalKey;

        // If Langfuse is configured, try to fetch from it first
        if (_langfuseOptions.Value.IsConfigured())
        {
            try
            {
                _logger.LogInformation("Fetching prompt '{LogicalKey}' -> '{ActualKey}' (version: {Version}, label: {Label}) from Langfuse",
                    logicalKey, actualKey, version, label);

                var langfusePrompt = await _langfuseService.GetPromptAsync(actualKey, version, label, cancellationToken);

                if (langfusePrompt != null)
                {
                    _logger.LogInformation("Successfully retrieved prompt '{LogicalKey}' from Langfuse", logicalKey);
                    return new PromptResponse
                    {
                        PromptKey = langfusePrompt.Name,
                        Content = langfusePrompt.Prompt,
                        Version = langfusePrompt.Version,
                        Labels = langfusePrompt.Labels,
                        Tags = langfusePrompt.Tags,
                        Type = langfusePrompt.Type,
                        Config = langfusePrompt.Config,
                        Source = "Langfuse"
                    };
                }

                _logger.LogWarning("Prompt '{LogicalKey}' not found in Langfuse, falling back to local defaults", logicalKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving prompt '{LogicalKey}' from Langfuse, falling back to local defaults", logicalKey);
            }
        }
        else
        {
            _logger.LogInformation("Langfuse is not configured, using local defaults for prompt '{LogicalKey}'", logicalKey);
        }

        // Always fallback to local defaults using the logical key
        return GetPromptFromDefaults(logicalKey);
    }

    public async Task<List<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default)
    {
        if (!_langfuseOptions.Value.IsConfigured())
        {
            _logger.LogWarning("Cannot get all prompts - Langfuse is not configured");
            throw new InvalidOperationException("Langfuse is not configured. Cannot retrieve prompts list.");
        }

        try
        {
            _logger.LogInformation("Fetching all prompts from Langfuse");
            return await _langfuseService.GetAllPromptsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all prompts from Langfuse");
            throw;
        }
    }

    public async Task<List<PromptResponse>> GetPromptsAsync(
        IEnumerable<string> promptKeys,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        if (promptKeys is null)
        {
            throw new ArgumentNullException(nameof(promptKeys));
        }

        var keys = promptKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        if (keys.Count == 0)
        {
            throw new ArgumentException("At least one prompt key is required.", nameof(promptKeys));
        }

        var results = new List<PromptResponse>();

        foreach (var key in keys)
        {
            var prompt = await GetPromptAsync(key, label: label, cancellationToken: cancellationToken);
            if (prompt != null)
            {
                results.Add(prompt);
            }
        }

        return results;
    }

    public async Task<PromptResponse> UpdatePromptLabelsAsync(
        string promptKey,
        int version,
        UpdatePromptLabelsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptKey))
        {
            throw new ArgumentException("PromptKey is required.", nameof(promptKey));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!_langfuseOptions.Value.IsConfigured())
        {
            _logger.LogWarning("Cannot update prompt '{PromptKey}' - Langfuse is not configured", promptKey);
            throw new InvalidOperationException("Langfuse is not configured. Cannot update prompts.");
        }

        try
        {
            // Map logical key to actual langfuse key if configured
            var promptKeys = _defaultPromptsProvider.GetPromptKeys();
            var actualKey = promptKeys != null && promptKeys.TryGetValue(promptKey, out var mapped) ? mapped : promptKey;

            _logger.LogInformation("Updating labels for prompt '{PromptKey}' (actual: {ActualKey}) version {Version} in Langfuse",
                promptKey, actualKey, version);

            var updated = await _langfuseService.UpdatePromptLabelsAsync(actualKey, version, request, cancellationToken);

            return new PromptResponse
            {
                PromptKey = updated.Name,
                Content = updated.Prompt,
                Version = updated.Version,
                Labels = updated.Labels,
                Tags = updated.Tags,
                Type = updated.Type,
                Config = updated.Config,
                Source = "Langfuse"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update labels for prompt '{PromptKey}' version {Version}", promptKey, version);
            throw;
        }
    }

    private PromptResponse? GetPromptFromDefaults(string promptKey)
    {
        var defaults = _defaultPromptsProvider.GetDefaults();
        if (defaults == null || !defaults.TryGetValue(promptKey, out var content))
        {
            _logger.LogWarning("Prompt '{PromptKey}' not found in local defaults either", promptKey);
            return null;
        }

        _logger.LogInformation("Returning prompt '{PromptKey}' from local defaults", promptKey);
        return new PromptResponse
        {
            PromptKey = promptKey,
            Content = content,
            Source = "Local"
        };
    }
}
