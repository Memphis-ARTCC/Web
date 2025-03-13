using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Code))]
public class TrainingMilestone
{
    public int Id { get; set; }
    public required TrainingMilestoneTrack Track { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Facility { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}