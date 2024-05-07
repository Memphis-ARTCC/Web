namespace Memphis.Shared.Models;

public class TrainingSchedule
{
    public int Id { get; set; }
    public required User Instructor { get; set; }
    public User? Student { get; set; }
    public required ICollection<TrainingType> TrainingTypes { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End => Start.AddHours(1.5);
}