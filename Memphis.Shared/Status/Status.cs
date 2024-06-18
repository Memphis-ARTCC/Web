using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Status;

public class Data
{
    [JsonProperty("v3")]
    [JsonPropertyName("v3")]
    public required IList<string> V3 { get; set; }
}

public class Status
{
    [JsonProperty("data")]
    [JsonPropertyName("data")]
    public required Data Data { get; set; }
}
