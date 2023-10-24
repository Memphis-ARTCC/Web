namespace Memphis.Shared.Dtos;

public class NotificationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Link { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}