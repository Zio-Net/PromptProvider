using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record LangfusePromptModel
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("config")]
    public LangfusePromptConfiguration? Config { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("labels")]
    public required string[] Labels { get; set; }

    [JsonPropertyName("tags")]
    public required string[] Tags { get; set; }
}

public record LangfusePromptConfiguration
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("supported_languages")]
    public string[]? SupportedLanguages { get; set; }
}
