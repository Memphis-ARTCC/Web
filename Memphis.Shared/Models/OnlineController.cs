namespace Memphis.Shared.Models;

public class OnlineController
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Callsign { get; set; }
    public required string Frequency { get; set; }
    public TimeSpan Duration { get; set; }
}