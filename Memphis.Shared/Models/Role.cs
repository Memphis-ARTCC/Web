using System.Text.Json.Serialization;

namespace Memphis.Shared.Models;

public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NameShort { get; set; }
    public required string Email { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
}