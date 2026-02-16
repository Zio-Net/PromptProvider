using PromptProvider.Models;

namespace PromptProvider.Interfaces;

public interface ILangfuseService
{
    Task<LangfusePromptModel?> GetPromptAsync(
        string promptName,
        int? version = null,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<LangfuseChatPromptModel?> GetChatPromptAsync(
        string promptName,
        int? version = null,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default);

    Task<CreateLangfusePromptResponse> CreatePromptAsync(
        CreateLangfusePromptRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateLangfuseChatPromptResponse> CreateChatPromptAsync(
        CreateLangfuseChatPromptRequest request,
        CancellationToken cancellationToken = default);

    Task<LangfusePromptModel> UpdatePromptLabelsAsync(
        string promptName,
        int version,
        UpdatePromptLabelsRequest request,
        CancellationToken cancellationToken = default);
}
