using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class EventPosition
{
    public int Id { get; set; }
    public required Event Event { get; set; }
    public required string Name { get; set; }
    public Rating MinRating { get; set; }
    public bool Available { get; set; }
}