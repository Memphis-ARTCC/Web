using Memphis.Shared.Enums;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Datafeed;

public class Atis
{
    [JsonProperty("cid")]
    [JsonPropertyName("cid")]
    public int Cid { get; set; }

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonProperty("callsign")]
    [JsonPropertyName("callsign")]
    public required string Callsign { get; set; }

    [JsonProperty("frequency")]
    [JsonPropertyName("frequency")]
    public required string Frequency { get; set; }

    [JsonProperty("facility")]
    [JsonPropertyName("facility")]
    public required Facility Facility { get; set; }

    [JsonProperty("rating")]
    [JsonPropertyName("rating")]
    public required Rating Rating { get; set; }

    [JsonProperty("server")]
    [JsonPropertyName("server")]
    public required string Server { get; set; }

    [JsonProperty("visual_range")]
    [JsonPropertyName("visual_range")]
    public int VisualRange { get; set; }

    [JsonProperty("atis_code")]
    [JsonPropertyName("atis_code")]
    public string? AtisCode { get; set; }

    [JsonProperty("text_atis")]
    [JsonPropertyName("text_atis")]
    public required IList<string> TextAtis { get; set; }

    [JsonProperty("last_updated")]
    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }

    [JsonProperty("logon_time")]
    [JsonPropertyName("logon_time")]
    public DateTimeOffset LogonTime { get; set; }
}
