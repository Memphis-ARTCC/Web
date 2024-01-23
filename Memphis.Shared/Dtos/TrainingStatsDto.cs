using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class TrainingStatsDto
{
    public required IList<Dictionary<TrainingMilestone, int>> MilestoneCount { get; set; }
}