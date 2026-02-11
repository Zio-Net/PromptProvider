# PromptProvider

PromptProvider is a .NET SDK for managing prompts, with support for [Langfuse](https://langfuse.com/) integration and local/default prompt fallbacks. It provides a unified interface for fetching, creating, and managing prompts for LLM applications.

---

## Installation

```shell
# Using NuGet
 dotnet add package PromptProvider
```

---

## Configuration

You can use PromptProvider with Langfuse, with local configuration, or both. All options are supported via dependency injection.

### 1. Add to your DI container

In your `Program.cs` or startup:

```csharp
using PromptProvider;

builder.Services.AddPromptProvider(
    configureLangfuse: options => builder.Configuration.GetSection("Langfuse").Bind(options),
    configurePrompts: options => builder.Configuration.GetSection("Prompts").Bind(options),
    configurePromptKeys: options => builder.Configuration.GetSection("PromptKeys").Bind(options)
);
```

- `configureLangfuse`: (optional) Bind Langfuse API credentials and URL
- `configurePrompts`: (optional) Bind local prompt defaults
- `configurePromptKeys`: (optional) Map logical prompt names to Langfuse keys/labels. When a label is configured here, it will be used automatically when fetching prompts unless explicitly overridden.

### 2. Example `appsettings.json`

```json
{
  "Langfuse": {
    "BaseUrl": "https://api.langfuse.com",
    "PublicKey": "your-public-key",
    "SecretKey": "your-secret-key"
  },
  "Prompts": {
    "Defaults": {
      "WelcomePrompt": "Welcome to our system!",
      "ErrorPrompt": "An error occurred. Please try again."
    },
    "ChatDefaults": {
      "SupportChat": [
        { "Role": "system", "Content": "You are a helpful assistant." }
      ]
    }
  },
  "PromptKeys": {
    "ChatTitle": { "Key": "chat.title.generate", "Label": "production" },
    "SystemDefault": { "Key": "prompts.system.default", "Label": "production" }
  }
}
```

**Note on Labels**: When you configure a `Label` in `PromptKeys`, it will be automatically used when fetching that prompt. For example:
- If `ChatTitle` is configured with `"Label": "staging"`, calling `GetPromptAsync("ChatTitle")` will fetch the "staging" label from Langfuse
- You can still override this by explicitly passing a label: `GetPromptAsync("ChatTitle", label: "production")` will use the "production" label instead
- If no label is configured and none is provided, Langfuse will use its default behavior (typically "production")

---

## Usage Example

Inject `IPromptService` into your class:

```csharp
using PromptProvider.Interfaces;

public class MyService
{
    private readonly IPromptService _promptService;
    public MyService(IPromptService promptService)
    {
        _promptService = promptService;
    }

    public async Task<string> GetWelcomePromptAsync()
    {
        var prompt = await _promptService.GetPromptAsync("WelcomePrompt");
        return prompt?.Content ?? "Default fallback message.";
    }
}
```

### Fetching a chat prompt

```csharp
var chatPrompt = await _promptService.GetChatPromptAsync("SupportChat");
var messages = chatPrompt?.ChatMessages;
```

### Creating a new prompt in Langfuse

```csharp
await _promptService.CreatePromptAsync(new CreatePromptRequest {
    PromptKey = "MyPrompt",
    Content = "This is a new prompt.",
    CommitMessage = "Initial version"
});
```

---

## Options & Extension Points

- **Langfuse integration**: If Langfuse is configured, prompts are fetched/created remotely. If not, local defaults are used.
- **Local defaults**: Provide fallback prompts via configuration or a custom `IDefaultPromptsProvider`.
- **Prompt key mapping**: Use `PromptKeys` to map logical names to Langfuse keys/labels.
- **Custom providers**: You can implement `IDefaultPromptsProvider` for advanced scenarios.

---

## Interfaces

- `IPromptService`: Main interface for prompt operations (fetch, create, update, batch, etc.)
- `IDefaultPromptsProvider`: For providing local prompt defaults (can be replaced)

---

## License

MIT
