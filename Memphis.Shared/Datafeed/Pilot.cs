using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Memphis.Shared.Datafeed;

public enum PilotRating
{
    NEW,
    PPL,
    IR = 3,
    CMEL = 7,
    ATPL = 15,
    FI = 31,
    FE = 63
}

public enum MilitaryRating
{
    M0,
    M1,
    M2 = 3,
    M3 = 7,
    M4 = 15
}

public class FlightPlan
{
    [JsonProperty("flight_rules")]
    [JsonPropertyName("flight_rules")]
    public required string FlightRules { get; set; }

    [JsonProperty("aircraft")]
    [JsonPropertyName("aircraft")]
    public required string Aircraft { get; set; }

    [JsonProperty("aircraft_faa")]
    [JsonPropertyName("aircraft_faa")]
    public required string AircraftFaa { get; set; }

    [JsonProperty("aircraft_short")]
    [JsonPropertyName("aircraft_short")]
    public required string AircraftShort { get; set; }

    [JsonProperty("departure")]
    [JsonPropertyName("departure")]
    public required string Departure { get; set; }

    [JsonProperty("arrival")]
    [JsonPropertyName("arrival")]
    public required string Arrival { get; set; }

    [JsonProperty("alternate")]
    [JsonPropertyName("alternate")]
    public required string Alternate { get; set; }

    [JsonProperty("cruise_tas")]
    [JsonPropertyName("cruise_tas")]
    public required string CruiseTas { get; set; }

    [JsonProperty("altitude")]
    [JsonPropertyName("altitude")]
    public required string Altitude { get; set; }

    [JsonProperty("deptime")]
    [JsonPropertyName("deptime")]
    public required string DepTime { get; set; }

    [JsonProperty("enroute_time")]
    [JsonPropertyName("enroute_time")]
    public required string EnrouteTime { get; set; }

    [JsonProperty("fuel_time")]
    [JsonPropertyName("fuel_time")]
    public required string FuelTime { get; set; }

    [JsonProperty("remarks")]
    [JsonPropertyName("remarks")]
    public required string Remarks { get; set; }

    [JsonProperty("route")]
    [JsonPropertyName("route")]
    public required string Route { get; set; }

    [JsonProperty("revision_id")]
    [JsonPropertyName("revision_id")]
    public int RevisionId { get; set; }

    [JsonProperty("assigned_transponder")]
    [JsonPropertyName("assigned_transponder")]
    public int AssignedTransponder { get; set; }
}

public class Pilot
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

    [JsonProperty("server")]
    [JsonPropertyName("server")]
    public required string Server { get; set; }

    [JsonProperty("pilot_rating")]
    [JsonPropertyName("pilot_rating")]
    public required PilotRating PilotRating { get; set; }

    [JsonProperty("military_rating")]
    [JsonPropertyName("military_rating")]
    public required MilitaryRating MilitaryRating { get; set; }

    [JsonProperty("latitude")]
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonProperty("altitude")]
    [JsonPropertyName("altitude")]
    public int Altitude { get; set; }

    [JsonProperty("groundspeed")]
    [JsonPropertyName("groundspeed")]
    public int GroundSpeed { get; set; }

    [JsonProperty("transponder")]
    [JsonPropertyName("transponder")]
    public required string Transponder { get; set; }

    [JsonProperty("heading")]
    [JsonPropertyName("heading")]
    public int Heading { get; set; }

    [JsonProperty("qnh_i_hg")]
    [JsonPropertyName("qnh_i_hg")]
    public double QnhIhg { get; set; }

    [JsonProperty("qnh_mb")]
    [JsonPropertyName("qnh_mb")]
    public int QnhMb { get; set; }

    [JsonProperty("flight_plan")]
    [JsonPropertyName("flight_plan")]
    public FlightPlan? FlightPlan { get; set; }

    [JsonProperty("logon_time")]
    [JsonPropertyName("logon_time")]
    public DateTimeOffset LogonTime { get; set; }

    [JsonProperty("last_updated")]
    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }
}
