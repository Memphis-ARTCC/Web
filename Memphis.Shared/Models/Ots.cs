using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Status))]
public class Ots
{
    public int Id { get; set; }
    public required User Submitter { get; set; }
    public required User User { get; set; }
    public required User Instructor { get; set; }
    public DateTimeOffset? Start { get; set; }
    public required TrainingMilestone Milestone { get; set; }
    public required string Facility { get; set; }
    public OtsStatus Status { get; set; } = OtsStatus.PENDING;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}