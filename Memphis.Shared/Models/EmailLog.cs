namespace Memphis.Shared.Models;

public class EmailLog
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string To { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
