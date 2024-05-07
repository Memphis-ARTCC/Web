#region

using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Memphis.Shared.Models;

[Index(nameof(Status))]
[Index(nameof(Start))]
public class EventRegistration
{
    public int Id { get; set; }
    public required User User { get; set; }
    public required Event Event { get; set; }
    public required EventPosition EventPosition { get; set; }
    public EventRegistrationStatus Status { get; set; } = EventRegistrationStatus.PENDING;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}