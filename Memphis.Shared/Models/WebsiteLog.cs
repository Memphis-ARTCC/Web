namespace Memphis.Shared.Models;

public class WebsiteLog
{
    public int Id { get; set; }
    public string? Cid { get; set; }
    public string? Name { get; set; }
    public required string Action { get; set; }
    public required string Ip { get; set; }
    public required string OldData { get; set; }
    public required string NewData { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}