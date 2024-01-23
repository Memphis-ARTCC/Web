using Memphis.Shared.Enums;
using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class UpdateOtsDto
{
    public int Id { get; set; }
    public int? Instructor { get; set; }
    public DateTimeOffset? Start { get; set; }
    public required TrainingMilestone Milestone { get; set; }
    public required string Facility { get; set; }
    public OtsResult? Result { get; set; }
}

public class OtsDto
{
    public int Id { get; set; }
    public required RosterUserDto Submitter { get; set; }
    public required RosterUserDto User { get; set; }
    public RosterUserDto? Instructor { get; set; }
    public DateTimeOffset? Start { get; set; }
    public required TrainingMilestone Milestone { get; set; }
    public required string Facility { get; set; }
    public OtsStatus Status { get; set; } = OtsStatus.PENDING;
    public OtsResult? Result { get; set; }

    public static OtsDto Parse(Ots ots)
    {
        return new OtsDto
        {
            Id = ots.Id,
            Submitter = RosterUserDto.Parse(ots.Submitter),
            User = RosterUserDto.Parse(ots.User),
            Instructor = ots?.Instructor != null ? RosterUserDto.Parse(ots.Instructor) : null,
            Start = ots?.Start,
            Milestone = ots!.Milestone,
            Facility = ots.Facility,
            Status = ots.Status
        };
    }

    public static IList<OtsDto> ParseMany(IList<Ots> ots)
    {
        return ots.Select(Parse).ToList();
    }
}