namespace Memphis.Shared.Dtos;

public class HoursDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public required RosterUserDto User { get; set; }
    public double DeliveryHours { get; set; }
    public double GroundHours { get; set; }
    public double TowerHours { get; set; }
    public double ApproachHours { get; set; }
    public double CenterHours { get; set; }
    public double TotalHours => DeliveryHours + GroundHours + TowerHours + ApproachHours + CenterHours;
}