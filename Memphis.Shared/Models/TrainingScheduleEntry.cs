namespace Memphis.Shared.Models;

public class TrainingScheduleEntry
{
    public int Id { get; set; }
    public required User User { get; set; }
    public required TrainingSchedule TrainingSchedule { get; set; }
    public required TrainingType TrainingType { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}