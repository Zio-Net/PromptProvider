using Microsoft.Extensions.Logging;

namespace PromptProvider.Services;

internal static class PromptLoggingEvents
{
    public static readonly EventId LangfuseFetchPrompt = new(1001, nameof(LangfuseFetchPrompt));
    public static readonly EventId LangfuseFetchChatPrompt = new(1002, nameof(LangfuseFetchChatPrompt));
    public static readonly EventId LangfuseFallback = new(1003, nameof(LangfuseFallback));
    public static readonly EventId LocalDefaultReturned = new(1004, nameof(LocalDefaultReturned));
    public static readonly EventId BatchPromptItemFailed = new(1005, nameof(BatchPromptItemFailed));
    public static readonly EventId RetryAttempt = new(1006, nameof(RetryAttempt));
}
