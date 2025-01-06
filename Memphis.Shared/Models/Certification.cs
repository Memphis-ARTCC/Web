using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class Certification
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Solo { get; set; }
    public required CertificationLevel Level { get; set; }
}
