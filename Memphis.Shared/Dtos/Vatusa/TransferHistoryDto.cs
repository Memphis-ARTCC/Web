using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Vatusa;

public class TransferHistoryDto
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonPropertyName("cid")]
    [JsonProperty("cid")]
    public int Cid { get; set; }

    [JsonPropertyName("to")]
    [JsonProperty("to")]
    public required string To { get; set; }

    [JsonPropertyName("from")]
    [JsonProperty("from")]
    public required string From { get; set; }

    [JsonPropertyName("reason")]
    [JsonProperty("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonPropertyName("created_at")]
    [JsonProperty("created_at")]
    public DateTimeOffset Created { get; set; }
}
