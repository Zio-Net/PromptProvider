using PromptProvider.Models;

namespace PromptProvider.Interfaces;

public interface ILangfuseService
{
    /// <summary>
    /// Get a prompt by name from Langfuse API.
    /// </summary>
    /// <param name="promptName">The name of the prompt</param>
    /// <param name="version">Optional version of the prompt to retrieve</param>
    /// <param name="label">Optional label of the prompt (defaults to "production" if no label or version is set)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The Langfuse prompt model</returns>
    Task<LangfusePromptModel?> GetPromptAsync(
        string promptName,
        int? version = null,
        string? label = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all prompts from Langfuse API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Langfuse prompt list items</returns>
    Task<List<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new version for the prompt with the given name in Langfuse API.
    /// </summary>
    /// <param name="request">The prompt creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created prompt response</returns>
    Task<CreateLangfusePromptResponse> CreatePromptAsync(
        CreateLangfusePromptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update labels for a specific prompt version in Langfuse API.
    /// </summary>
    /// <param name="promptName">The name of the prompt</param>
    /// <param name="version">The version number to update</param>
    /// <param name="request">The update request containing new labels</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated prompt model</returns>
    Task<LangfusePromptModel> UpdatePromptLabelsAsync(
        string promptName,
        int version,
        UpdatePromptLabelsRequest request,
        CancellationToken cancellationToken = default);
}
