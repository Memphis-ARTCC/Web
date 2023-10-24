using Memphis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(FirstName))]
[Index(nameof(LastName))]
[Index(nameof(Email))]
[Index(nameof(Rating))]
[Index(nameof(Status))]
public class User
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Initials { get; set; }
    public required string Email { get; set; }
    public Rating Rating { get; set; }
    public DateTimeOffset Joined { get; set; }
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    public bool Visitor { get; set; }
    public string? VisitorFrom { get; set; }
    public bool CanRegisterForEvents { get; set; } = true;
    public bool CanRequestTraining { get; set; } = true;
    public ICollection<Role>? Roles { get; set; }
    public AirportCert Minor { get; set; } = AirportCert.NONE;
    public AirportCert Major { get; set; } = AirportCert.NONE;
    public CenterCert Center { get; set; } = CenterCert.NONE;
    public string? DiscordId { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}