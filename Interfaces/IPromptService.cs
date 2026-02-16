using PromptProvider.Models;

namespace PromptProvider.Interfaces;

public interface IPromptService
{
    Task<PromptResponse> CreatePromptAsync(CreatePromptRequest request, CancellationToken cancellationToken = default);

    Task<ChatPromptResponse> CreateChatPromptAsync(CreateChatPromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a text prompt.
    /// Precedence rules:
    /// 1) explicit method parameters (version/label)
    /// 2) configured prompt entry defaults
    /// 3) Langfuse default behavior.
    /// If a version is effective, label is ignored.
    /// Falls back to local configured defaults when remote retrieval fails.
    /// </summary>
    Task<PromptResponse?> GetPromptAsync(string promptKey, int? version = null, string? label = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a chat prompt.
    /// Precedence rules:
    /// 1) explicit method parameters (version/label)
    /// 2) configured prompt entry defaults
    /// 3) Langfuse default behavior.
    /// If a version is effective, label is ignored.
    /// Falls back to local configured defaults when remote retrieval fails.
    /// </summary>
    Task<ChatPromptResponse?> GetChatPromptAsync(string promptKey, int? version = null, string? label = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves prompts by keys. Individual key failures do not fail the whole batch.
    /// </summary>
    Task<IReadOnlyList<PromptResponse>> GetPromptsAsync(IEnumerable<string> promptKeys, string? label = null, CancellationToken cancellationToken = default);

    Task<PromptResponse> UpdatePromptLabelsAsync(string promptKey, int version, UpdatePromptLabelsRequest request, CancellationToken cancellationToken = default);
}
