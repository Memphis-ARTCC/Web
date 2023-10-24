using Memphis.Shared.Enums;

namespace Memphis.Shared.Dtos;

public class OtsDto
{
    public int UserId { get; set; }
    public int InstructorId { get; set; }
    public int TrainingRequestId { get; set; }
    public DateTimeOffset? Start { get; set; }
    public int MilestoneId { get; set; }
    public TrainingTicketPosition Position { get; set; }
    public required string Facility { get; set; }
    public OtsStatus Status { get; set; } = OtsStatus.PENDING;
}