using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class File
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Version { get; set; }
    public string? FileUrl { get; set; }
    public FileType Type { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}

public class FilePayload
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Version { get; set; }
    public FileType Type { get; set; }
}