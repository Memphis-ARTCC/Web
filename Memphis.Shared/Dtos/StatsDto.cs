using Memphis.Shared.Enums;

namespace Memphis.Shared.Dtos;

public class StatsDto
{
    public int Cid { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserStatus Status { get; set; }
    public Rating Rating { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double DeliveryHours { get; set; }
    public double GroundHours { get; set; }
    public double TowerHours { get; set; }
    public double TraconHours { get; set; }
    public double CenterHours { get; set; }
    public double TotalHours { get; set; }
}
