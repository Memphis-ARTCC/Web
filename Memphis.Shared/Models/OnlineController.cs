using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class OnlineController
{
    public int Id { get; set; }
    public int Cid { get; set; }
    public required string Name { get; set; }
    public required Rating Rating { get; set; }
    public required string Callsign { get; set; }
    public required string Frequency { get; set; }
    public required string Duration { get; set; }
}