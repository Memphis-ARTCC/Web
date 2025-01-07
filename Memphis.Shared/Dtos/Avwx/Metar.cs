using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Avwx;

public class Altimeter
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class Cloud
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("altitude")]
    public required int Altitude { get; set; }

    [JsonPropertyName("modifier")]
    public string? Modifier { get; set; }
}

public class Temperature
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class Visibility
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class WindDirection
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class WindGust
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class WindSpeed
{
    [JsonPropertyName("repr")]
    public required string Repr { get; set; }
}

public class Metar
{
    [JsonPropertyName("altimeter")]
    public required Altimeter Altimeter { get; set; }

    [JsonPropertyName("clouds")]
    public required IList<Cloud> Clouds { get; set; }

    [JsonPropertyName("flight_rules")]
    public required string FlightRules { get; set; }

    [JsonPropertyName("raw")]
    public required string Raw { get; set; }

    [JsonPropertyName("temperature")]
    public required Temperature Temperature { get; set; }

    [JsonPropertyName("visibility")]
    public required Visibility Visibility { get; set; }

    [JsonPropertyName("wind_direction")]
    public required WindDirection WindDirection { get; set; }

    [JsonPropertyName("wind_gust")]
    public required WindGust WindGust { get; set; }

    [JsonPropertyName("wind_speed")]
    public required WindSpeed WindSpeed { get; set; }
}
