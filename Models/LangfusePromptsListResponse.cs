using System.Text.Json.Serialization;

namespace PromptProvider.Models;

public record LangfusePromptsListResponse
{
    [JsonPropertyName("data")]
    public List<LangfusePromptListItem> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public LangfuseListMeta? Meta { get; set; }

    [JsonPropertyName("pagination")]
    public LangfuseListMeta? Pagination { get; set; }
}

public record LangfuseListMeta
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
