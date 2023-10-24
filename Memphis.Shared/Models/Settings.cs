namespace Memphis.Shared.Models;

public class Settings
{
    public int Id { get; set; }
    public bool VisitingOpen { get; set; }
    public int RequiredHours { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}