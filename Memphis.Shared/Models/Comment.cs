namespace Memphis.Shared.Models;

public class Comment
{
    public int Id { get; set; }
    public required User User { get; set; }
    public required User Submitter { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class CommentPayload
{
    public int UserId { get; set; }
    public required string Message { get; set; }
}