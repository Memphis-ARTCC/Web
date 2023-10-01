using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Position))]
[Index(nameof(Performance))]
public class TrainingTicket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required User User { get; set; }
    public int TrainerId { get; set; }
    public required User Trainer { get; set; }
    public int TrainingRequestId { get; set; }
    public required TrainingRequest TrainingRequest { get; set; }
    public TrainingTicketPosition Position { get; set; }
    public required string Facility { get; set; }
    public TrainingTicketPerformance Performance { get; set; }
    public required string UserNotes { get; set; }
    public required string TrainingNotes { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}
