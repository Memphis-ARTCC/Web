namespace Memphis.Shared.Models;

public class News
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}

public class NewsPayload
{
    public required string Title { get; set; }
    public required string Content { get; set; }
}
