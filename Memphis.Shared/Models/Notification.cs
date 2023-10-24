using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Title))]
[Index(nameof(Read))]
public class Notification
{
    public int Id { get; set; }
    public required User User { get; set; }
    public required string Title { get; set; }
    public required string Link { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}