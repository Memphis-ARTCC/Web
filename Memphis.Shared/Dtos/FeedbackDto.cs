using Memphis.Shared.Enums;

namespace Memphis.Shared.Dtos;

public class FeedbackDto
{
    public int Id { get; set; }
    public int ControllerId { get; set; }
    public required string ControllerCallsign { get; set; }
    public required string Description { get; set; }
    public FeedbackLevel Level { get; set; }
    public string? Reply { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.PENDING;
}