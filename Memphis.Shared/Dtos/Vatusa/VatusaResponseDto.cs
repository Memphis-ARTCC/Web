using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Vatusa;

public class VatusaResponseDto<T>
{
    [JsonPropertyName("data")]
    [JsonProperty("data")]
    public required T Data { get; set; }
}
