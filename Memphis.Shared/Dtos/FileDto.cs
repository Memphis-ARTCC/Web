using Memphis.Shared.Enums;

namespace Memphis.Shared.Dtos;

public class FileDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Version { get; set; }
    public FileType Type { get; set; }
}