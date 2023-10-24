using Memphis.Shared.Enums;
using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class RosterUserDto
{
    public int Cid { get; set; }
    public required string Name { get; set; }
    public required string Initials { get; set; }
    public required string Rating { get; set; }
    public UserStatus Status { get; set; }
    public bool Visitor { get; set; }
    public string? VisitorFrom { get; set; }
    public AirportCert Minor { get; set; }
    public AirportCert Major { get; set; }
    public CenterCert Center { get; set; }
    public required IList<Role> Roles { get; set; }
}