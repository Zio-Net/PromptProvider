# PromptProvider

PromptProvider is a .NET SDK for **prompt fetching and prompt version management** with:

- Langfuse integration for remote prompt storage/retrieval
- Local defaults as fallback for reliability
- Logical prompt keys mapped to actual Langfuse keys
- Optional label/version defaults per prompt

It is designed so your application code can ask for prompts by logical names while PromptProvider resolves mapping, label/version precedence, and fallback behavior.

---

## Installation

```shell
dotnet add package PromptProvider
```

---

## Quick start

Register PromptProvider in DI:

```csharp
using PromptProvider;

builder.Services.AddPromptProvider(
    configureLangfuse: options => builder.Configuration.GetSection("Langfuse").Bind(options),
    configurePrompts: options => builder.Configuration.GetSection("Prompts").Bind(options)
    // configurePromptKeys is still supported for legacy split configuration
);
```

Inject and use `IPromptService`:

```csharp
using PromptProvider.Interfaces;

public class MyService
{
    private readonly IPromptService _promptService;

    public MyService(IPromptService promptService)
    {
        _promptService = promptService;
    }

    public async Task<string> GetWelcomeAsync(CancellationToken cancellationToken)
    {
        var prompt = await _promptService.GetPromptAsync("WelcomePrompt", cancellationToken: cancellationToken);
        return prompt?.Content ?? "Fallback text";
    }
}
```

---

## Terminology: logical name vs actual name

This is a great question — here is the exact meaning:

- **Logical name** (aka app key): the friendly key used in your code, for example `"WelcomePrompt"`.
- **Actual name** (aka Langfuse key): the real prompt identifier stored in Langfuse, for example `"prompts.welcome"`.

So your app can call:

```csharp
await _promptService.GetPromptAsync("WelcomePrompt", cancellationToken: ct);
```

And PromptProvider internally resolves it to the actual Langfuse key:

```text
WelcomePrompt -> prompts.welcome
```

This mapping comes from your `Prompts.Entries` configuration (`Name` = logical name, `Key` = actual Langfuse name).

---

## Configuration

PromptProvider supports both:

1. **Unified configuration (recommended)**
2. **Legacy split configuration** (`Prompts.Defaults`, `Prompts.ChatDefaults`, `PromptKeys`)

### Unified configuration (recommended)

```json
{
  "Langfuse": {
    "BaseUrl": "https://api.langfuse.com",
    "PublicKey": "your-public-key",
    "SecretKey": "your-secret-key",
    "HttpClient": {
      "MaxConnectionsPerServer": 50,
      "PooledConnectionLifetimeMinutes": 10,
      "RequestTimeoutSeconds": 30
    },
    "Resilience": {
      "Enabled": true,
      "MaxRetries": 2,
      "BaseDelayMs": 200
    }
  },
  "Prompts": {
    "Entries": [
      {
        "Name": "WelcomePrompt",
        "Key": "prompts.welcome",
        "Label": "production",
        "Default": "Welcome to our system!"
      },
      {
        "Name": "SupportChat",
        "Key": "chat.support",
        "ChatDefault": [
          { "Role": "system", "Content": "You are a helpful assistant." }
        ]
      }
    ]
  }
}
```

### Entry fields

Each prompt entry can include:

- `Name`: logical/app key used in your code (`GetPromptAsync("Name")`)
- `Key`: actual Langfuse prompt key stored remotely
- `Label`: default label when caller does not pass one
- `Version`: default version when caller does not pass one
- `Default`: local text fallback prompt
- `ChatDefault`: local chat fallback prompt

All fields are optional except whatever your scenario needs. Example combinations:

- Key + label only
- Local default only
- Key + default + label
- Key + version + fallback

### Legacy split configuration (still supported)

You can continue using:

- `Prompts.Defaults`
- `Prompts.ChatDefaults`
- `PromptKeys`

PromptProvider merges these with unified entries when building resolved prompt configuration.

---

## Behavior and precedence

For `GetPromptAsync` and `GetChatPromptAsync`, precedence is:

1. Explicit method args (`version`, `label`)
2. Configured defaults from `Prompts.Entries` / `Prompts.PromptEntries` (or legacy `PromptKeys`)
3. Langfuse default behavior

### Important rule

If an effective **version** exists, **label is ignored**.

### Fallback behavior

- PromptProvider first tries Langfuse (when configured).
- If fetch fails or prompt is unavailable, PromptProvider returns local defaults (if configured).
- If no local fallback exists, result is `null`.

---

## Langfuse HTTP and resilience options

These are optional and configurable by SDK consumers.

### `Langfuse.HttpClient`

- `MaxConnectionsPerServer`
- `PooledConnectionLifetimeMinutes`
- `RequestTimeoutSeconds`

Use these to tune throughput and connection reuse based on your traffic profile.

### `Langfuse.Resilience`

- `Enabled` (default `false`)
- `MaxRetries` (default `2`)
- `BaseDelayMs` (default `200`)

When enabled, transient HTTP errors are retried with exponential backoff.

---

## API usage examples

### Fetch a text prompt

```csharp
var prompt = await _promptService.GetPromptAsync("WelcomePrompt", label: "production", cancellationToken: ct);
if (prompt is not null)
{
    Console.WriteLine(prompt.Content);
    Console.WriteLine(prompt.Source); // PromptSource.Langfuse or PromptSource.Local
}
```

### Fetch a chat prompt

```csharp
var chatPrompt = await _promptService.GetChatPromptAsync("SupportChat", cancellationToken: ct);
var messages = chatPrompt?.ChatMessages;
```

### Batch fetch with partial failure tolerance

```csharp
var prompts = await _promptService.GetPromptsAsync(
    new[] { "WelcomePrompt", "SystemPrompt", "UnknownPrompt" },
    cancellationToken: ct);

// Successful prompts are returned even if some keys fail.
```

### Create a text prompt version in Langfuse

```csharp
await _promptService.CreatePromptAsync(new CreatePromptRequest
{
    PromptKey = "WelcomePrompt",
    Content = "Welcome to our system!",
    CommitMessage = "Update welcome text"
}, ct);
```

### Create a chat prompt version in Langfuse

```csharp
await _promptService.CreateChatPromptAsync(new CreateChatPromptRequest
{
    PromptKey = "SupportChat",
    ChatMessages =
    [
        new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
        new ChatMessage { Role = "user", Content = "How can I reset my password?" }
    ],
    CommitMessage = "Improve support flow"
}, ct);
```

### Update labels for a specific version

```csharp
await _promptService.UpdatePromptLabelsAsync(
    promptKey: "WelcomePrompt",
    version: 3,
    request: new UpdatePromptLabelsRequest { NewLabels = ["production"] },
    cancellationToken: ct);
```

---

## Notes

- `GetPromptsAsync` executes prompt fetches concurrently.
- Individual key failures do not fail the full batch.
- Strong typing is used in responses:
  - `PromptSource` (`Local`, `Langfuse`)
  - `PromptKind` (`Text`, `Chat`, `Unknown`)
- Langfuse options are validated at startup (fail-fast for partial/invalid configuration).

---

## License

MIT
