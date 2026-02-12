namespace PromptProvider.Models;

public enum PromptSource
{
    Local = 0,
    Langfuse = 1
}

public enum PromptKind
{
    Text = 0,
    Chat = 1,
    Unknown = 2
}
