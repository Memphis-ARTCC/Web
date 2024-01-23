using Newtonsoft.Json;

namespace Memphis.Shared.Models;

public class TrainingType
{
    public int Id { get; set; }
    public required string Name { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public required ICollection<TrainingSchedule> Schedules { get; set; }
}