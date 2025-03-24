namespace Memphis.Shared.Models;

public class StaffingRequest
{
    public int Id { get; set; }
    public int Cid { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Organization { get; set; }
    public int EstimatedPilots { get; set; }
    public DateTimeOffset Start { get; set; }
    public TimeSpan Duration { get; set; }
}
