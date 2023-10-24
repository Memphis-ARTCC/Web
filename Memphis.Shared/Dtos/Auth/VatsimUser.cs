using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.auth;

public class PersonalDetails
{
    [JsonPropertyName("name_first")]
    [JsonProperty("name_first")]
    public required string NameFirst { get; set; }

    [JsonPropertyName("name_last")]
    [JsonProperty("name_last")]
    public required string NameLast { get; set; }

    [JsonPropertyName("name_full")]
    [JsonProperty("name_full")]
    public required string NameFull { get; set; }

    [JsonPropertyName("email")]
    [JsonProperty("email")]
    public required string Email { get; set; }
}

public class ControllerRating
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonPropertyName("long")]
    [JsonProperty("long")]
    public required string Long { get; set; }

    [JsonPropertyName("short")]
    [JsonProperty("short")]
    public required string Short { get; set; }
}

public class PilotRating
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonPropertyName("long")]
    [JsonProperty("long")]
    public required string Long { get; set; }

    [JsonPropertyName("short")]
    [JsonProperty("short")]
    public required string Short { get; set; }
}

public class Division
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public required string Name { get; set; }
}

public class Region
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public required string Name { get; set; }
}

public class Subdivision
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public required string Name { get; set; }
}

public class VatsimDetails
{
    [JsonPropertyName("rating")]
    [JsonProperty("rating")]
    public required ControllerRating ControllerRating { get; set; }

    [JsonPropertyName("pilotrating")]
    [JsonProperty("pilotrating")]
    public required PilotRating PilotRating { get; set; }

    [JsonPropertyName("division")]
    [JsonProperty("division")]
    public required Division Division { get; set; }

    [JsonPropertyName("region")]
    [JsonProperty("region")]
    public required Region Region { get; set; }

    [JsonPropertyName("subdivision")]
    [JsonProperty("subdivision")]
    public Subdivision? Subdivision { get; set; }
}

public class VatsimUserDto
{
    [JsonPropertyName("cid")]
    [JsonProperty("cid")]
    public int Cid { get; set; }

    [JsonPropertyName("personal")]
    [JsonProperty("personal")]
    public required PersonalDetails PersonalDetails { get; set; }

    [JsonPropertyName("vatsim")]
    [JsonProperty("vatsim")]
    public required VatsimDetails VatsimDetails { get; set; }
}

public class VatsimUserResponseDto
{
    [JsonPropertyName("data")]
    [JsonProperty("data")]
    public required VatsimUserDto Data { get; set; }
}