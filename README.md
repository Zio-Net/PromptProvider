# PromptProvider

PromptProvider — a library for managing prompts with Langfuse integration and support for default prompts.

## Installation

```powershell
dotnet add package PromptProvider
```

## Usage

```csharp
services.AddPromptProvider(
    configureLangfuse: options => Configuration.GetSection("Langfuse").Bind(options),
    configurePrompts: options => Configuration.GetSection("Prompts").Bind(options)
);
```

If you want to provide default prompts from configuration, register a provider or configure `Prompts` section and use the built-in `ConfigurationDefaultPromptsProvider`.

## Configuration example

`appsettings.json` example for `Prompts` section:

```json
{
  "Prompts": {
    "Defaults": {
      "WelcomePrompt": "Welcome to our system!",
      "ErrorPrompt": "An error occurred. Please try again."
    }
  }
}