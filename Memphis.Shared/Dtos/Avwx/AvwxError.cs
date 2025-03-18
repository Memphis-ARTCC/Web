using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Avwx;

public class AvwxError
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }
}
