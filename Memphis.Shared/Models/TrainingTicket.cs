using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Performance))]
public class TrainingTicket
{
    public int Id { get; set; }
    public required User User { get; set; }
    public required User Trainer { get; set; }
    public required TrainingMilestone Milestone { get; set; }
    public TrainingTicketPerformance Performance { get; set; }
    public required string UserNotes { get; set; }
    public required string TrainingNotes { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}