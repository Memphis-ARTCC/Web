using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class SoloCert
{
    public int Id { get; set; }
    public required User Submitted { get; set; }
    public required User User { get; set; }
    public AirportCert Tier2 { get; set; }
    public CenterCert Center { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}
