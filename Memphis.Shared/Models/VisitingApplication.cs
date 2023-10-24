using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class VisitingApplication
{
    public int Id { get; set; }
    public int Cid { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public Rating Rating { get; set; }
    public VisitingApplicationStatus Status { get; set; }
    public string? DenialReason { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}