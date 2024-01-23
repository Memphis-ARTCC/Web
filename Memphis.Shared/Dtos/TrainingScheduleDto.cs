namespace Memphis.Shared.Dtos;

public class TrainingScheduleDto
{
    public int Id { get; set; }
    public required IList<int> TrainingTypes { get; set; }
    public DateTimeOffset Start { get; set; }
}