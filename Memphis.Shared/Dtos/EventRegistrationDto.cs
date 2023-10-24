namespace Memphis.Shared.Dtos;

public class EventRegistrationDto
{
    public int EventId { get; set; }
    public int EventPositionId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}