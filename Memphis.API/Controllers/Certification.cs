using Memphis.Shared.Enums;

namespace Memphis.API.Controllers;

public class Certification
{
    public int Id { get; set; }
    public int Order { get; set; }
    public required string Name { get; set; }
    public required string Identifier { get; set; }
    public required string Description { get; set; }
    public bool Solo { get; set; }
    public Rating RequiredRating { get; set; }
}
