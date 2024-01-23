namespace Memphis.Shared.Dtos;

public class AirportDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Icao { get; set; }
}