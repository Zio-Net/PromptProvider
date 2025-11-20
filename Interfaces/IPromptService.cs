using PromptProvider.Models;

namespace PromptProvider.Interfaces;

public interface IPromptService
{
    /// <summary>
    /// Create a new prompt version in Langfuse
    /// </summary>
    Task<PromptResponse> CreatePromptAsync(CreatePromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a prompt by key with optional version or label. Falls back to local defaults if Langfuse is unavailable.
    /// </summary>
    /// <param name="promptKey">The prompt key/name</param>
    /// <param name="version">Optional specific version number</param>
    /// <param name="label">Optional label (e.g., "production", "latest")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<PromptResponse?> GetPromptAsync(string promptKey, int? version = null, string? label = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all prompts from Langfuse
    /// </summary>
    Task<List<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple prompts by keys with optional label. Falls back to local defaults if Langfuse is unavailable.
    /// </summary>
    Task<List<PromptResponse>> GetPromptsAsync(IEnumerable<string> promptKeys, string? label = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update labels for a specific prompt version in Langfuse
    /// </summary>
    Task<PromptResponse> UpdatePromptLabelsAsync(string promptKey, int version, UpdatePromptLabelsRequest request, CancellationToken cancellationToken = default);
}
