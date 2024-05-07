using Memphis.Shared.Enums;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;

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

    public static RosterUserDto Parse(User user)
    {
        return new RosterUserDto
        {
            Cid = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Initials = user.Initials,
            Rating = Helpers.GetRatingName(user.Rating),
            Status = user.Status,
            Visitor = user.Visitor,
            VisitorFrom = user.VisitorFrom,
            Minor = user.Minor,
            Major = user.Major,
            Roles = user.Roles?.ToList() ?? new List<Role>()
        };
    }

    public static IList<RosterUserDto> ParseMany(IList<User> users)
    {
        return users.Select(Parse).ToList();
    }
}