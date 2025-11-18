using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record CreateLangfusePromptResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("config")]
    public object? Config { get; set; }

    [JsonPropertyName("labels")]
    public required string[] Labels { get; set; }

    [JsonPropertyName("tags")]
    public required string[] Tags { get; set; }

    [JsonPropertyName("commitMessage")]
    public string? CommitMessage { get; set; }

    [JsonPropertyName("resolutionGraph")]
    public object? ResolutionGraph { get; set; }
}
