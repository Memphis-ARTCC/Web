using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Start))]
[Index(nameof(Status))]
public class TrainingRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required User User { get; set; }
    public TrainingTicketPosition Position { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public TrainingRequestStatus Status { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}
