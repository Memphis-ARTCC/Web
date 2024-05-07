using Memphis.Shared.Enums;
using System.Text.Json.Serialization;

namespace Memphis.Jobs.Dtos;

public class RoleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("cid")]
    public int Cid { get; set; }

    [JsonPropertyName("facility")]
    public required string Facility { get; set; }

    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class RosterDto
{
    [JsonPropertyName("cid")]
    public int Cid { get; set; }

    [JsonPropertyName("fname")]
    public required string FirstName { get; set; }

    [JsonPropertyName("lname")]
    public required string LastName { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("facility")]
    public required string Facility { get; set; }

    [JsonPropertyName("phone")]
    public Rating Rating { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("facility_join")]
    public DateTimeOffset FacilityJoin { get; set; }

    [JsonPropertyName("flag_homecontroller")]
    public bool HomeController { get; set; }

    [JsonPropertyName("roles")]
    public required IList<RoleDto> Roles { get; set; }

    [JsonPropertyName("last_promotion")]
    public DateTimeOffset LastPromotion { get; set; }

    [JsonPropertyName("membership")]
    public required string Membership { get; set; }
}

public class RosterResponseDto
{
    [JsonPropertyName("data")]
    public required IList<RosterDto> Data { get; set; }
}
