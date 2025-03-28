using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Avwx;

public class Altimeter
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class Cloud
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }

    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public required string Type { get; set; }

    [JsonPropertyName("altitude")]
    [JsonProperty("altitude")]
    public required int Altitude { get; set; }

    [JsonPropertyName("modifier")]
    [JsonProperty("modifier")]
    public string? Modifier { get; set; }
}

public class Temperature
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class Visibility
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class WindDirection
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class WindGust
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class WindSpeed
{
    [JsonPropertyName("repr")]
    [JsonProperty("repr")]
    public required string Repr { get; set; }
}

public class AvwxResponse
{
    [JsonPropertyName("altimeter")]
    [JsonProperty("altimeter")]
    public required Altimeter Altimeter { get; set; }

    [JsonPropertyName("clouds")]
    [JsonProperty("clouds")]
    public required IList<Cloud> Clouds { get; set; }

    [JsonPropertyName("flight_rules")]
    [JsonProperty("flight_rules")]
    public required string FlightRules { get; set; }

    [JsonPropertyName("raw")]
    [JsonProperty("raw")]
    public required string Raw { get; set; }

    [JsonPropertyName("temperature")]
    [JsonProperty("temperature")]
    public required Temperature Temperature { get; set; }

    [JsonPropertyName("visibility")]
    [JsonProperty("visibility")]
    public required Visibility Visibility { get; set; }

    [JsonPropertyName("wind_direction")]
    [JsonProperty("wind_direction")]
    public required WindDirection WindDirection { get; set; }

    [JsonPropertyName("wind_gust")]
    [JsonProperty("wind_gust")]
    public required WindGust WindGust { get; set; }

    [JsonPropertyName("wind_speed")]
    [JsonProperty("wind_speed")]
    public required WindSpeed WindSpeed { get; set; }
}
