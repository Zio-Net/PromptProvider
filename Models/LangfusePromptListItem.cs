using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record LangfusePromptListItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("tags")]
    public required string[] Tags { get; set; }

    [JsonPropertyName("labels")]
    public required string[] Labels { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public string? LastUpdatedAt { get; set; }

    [JsonPropertyName("versions")]
    public int[]? Versions { get; set; }

    [JsonPropertyName("lastConfig")]
    public object? LastConfig { get; set; }
}
