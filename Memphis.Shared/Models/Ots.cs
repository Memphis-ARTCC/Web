using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Position))]
[Index(nameof(Status))]
public class Ots
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int SubmitterId { get; set; }
    public required User Submitter { get; set; }
    public int? InstructorId { get; set; }
    public User? Instructor { get; set; }
    public int? TrainingRequestId { get; set; }
    public DateTimeOffset? Start { get; set; }
    public TrainingTicketPosition Position { get; set; }
    public required string Facility { get; set; }
    public OtsStatus Status { get; set; } = OtsStatus.PENDING;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}
