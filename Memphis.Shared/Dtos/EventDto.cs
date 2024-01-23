namespace Memphis.Shared.Dtos;

public class EventDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Host { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public bool IsOpen { get; set; }
}