using Memphis.Shared.Enums;
using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class RosterUserDto
{
    public int Cid { get; set; }
    public required string Name { get; set; }
    public required string Initials { get; set; }
    public Rating Rating { get; set; }
    public UserStatus Status { get; set; }
    public bool Visitor { get; set; }
    public string? VisitorFrom { get; set; }
    public Certification? Ground { get; set; }
    public Certification? Tower { get; set; }
    public Certification? Radar { get; set; }
    public Certification? Center { get; set; }
    public required IList<Role> Roles { get; set; }

    public static RosterUserDto Parse(User user)
    {
        return new RosterUserDto
        {
            Cid = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Initials = user.Initials,
            Rating = user.Rating,
            Status = user.Status,
            Visitor = user.Visitor,
            VisitorFrom = user.VisitorFrom,
            Ground = user.Ground,
            Tower = user.Tower,
            Radar = user.Radar,
            Center = user.Center,
            Roles = user.Roles?.ToList() ?? [],
        };
    }

    public static IList<RosterUserDto> ParseMany(IList<User> users)
    {
        return users.Select(Parse).ToList();
    }
}