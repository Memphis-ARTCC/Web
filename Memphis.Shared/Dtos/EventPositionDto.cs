using Memphis.Shared.Enums;

namespace Memphis.Shared.Dtos;

public class EventPositionDto
{
    public int EventId { get; set; }
    public required string Name { get; set; }
    public Rating MinRating { get; set; }
}