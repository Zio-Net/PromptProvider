using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record UpdatePromptLabelsRequest
{
    [JsonPropertyName("newLabels")]
    public required string[] NewLabels { get; set; }
}
