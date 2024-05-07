using Memphis.Shared.Enums;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Dtos.Vatusa;

public class TransferRequestDto
{
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public required string Status { get; set; }

    [JsonPropertyName("transfers")]
    [JsonProperty("transfers")]
    public required IList<TransferRequestBody> Transfers { get; set; }
}

public class TransferFrom
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public required string Name { get; set; }
}

public class TransferRequestBody
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonPropertyName("cid")]
    [JsonProperty("cid")]
    public int Cid { get; set; }

    [JsonPropertyName("fname")]
    [JsonProperty("fname")]
    public required string FirstName { get; set; }

    [JsonPropertyName("lname")]
    [JsonProperty("lname")]
    public required string LastName { get; set; }

    [JsonPropertyName("email")]
    [JsonProperty("email")]
    public required string Email { get; set; }

    [JsonPropertyName("reason")]
    [JsonProperty("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("intRating")]
    [JsonProperty("intRating")]
    public Rating Rating { get; set; }

    [JsonPropertyName("date")]
    [JsonProperty("date")]
    public DateTimeOffset Date { get; set; }

    [JsonPropertyName("fromFac")]
    [JsonProperty("fromFac")]
    public required TransferFrom FromFacility { get; set; }
}

public class TransferRequestActionDto
{
    public required string Action { get; set; }
    public string? Reason { get; set; }
}