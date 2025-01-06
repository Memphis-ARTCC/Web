namespace Memphis.Shared.Models;

public class Facility
{
    public int Id { get; set; }
    public required string Identifier { get; set; }
}

public class FacilityPayload
{
    public required string Identifier { get; set; }
}
