namespace Memphis.Shared.Models;

public class TrainingSchedule
{
    public int Id { get; set; }
    public required User User { get; set; }
    public User? Student { get; set; }
    public required TrainingType Type { get; set; }
    public TrainingType? SelectedType { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End => Start.AddHours(1.5);
}