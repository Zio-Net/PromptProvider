using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record PromptResponse
{
    public required string PromptKey { get; set; }
    public required string Content { get; set; }
    public int? Version { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Tags { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PromptKind Type { get; set; } = PromptKind.Text;
    public LangfusePromptConfiguration? Config { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PromptSource Source { get; set; }
}
