using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos;

public class VatsimTokenDto
{
    [JsonPropertyName("access_token")]
    [JsonProperty("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    [JsonProperty("refresh_token")]
    public required string RefreshToken { get; set; }

    [JsonPropertyName("scopes")]
    [JsonProperty("scopes")]
    public required List<string> Scopes { get; set; }

    [JsonPropertyName("token_type")]
    [JsonProperty("token_type")]
    public required string TokenType { get; set; }
}
