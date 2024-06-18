using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Datafeed;

public class Datafeed
{
    [JsonProperty("pilots")]
    [JsonPropertyName("pilots")]
    public required IList<Pilot> Pilots { get; set; }

    [JsonProperty("controllers")]
    [JsonPropertyName("controllers")]
    public required IList<Controller> Controllers { get; set; }

    [JsonProperty("atis")]
    [JsonPropertyName("atis")]
    public required IList<Atis> Atis { get; set; }
}
