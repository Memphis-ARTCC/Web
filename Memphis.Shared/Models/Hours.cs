using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(Month))]
[Index(nameof(Year))]
public class Hours
{
    public int Id { get; set; }
    public required User User { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double DeliveryHours { get; set; }
    public double GroundHours { get; set; }
    public double TowerHours { get; set; }
    public double TraconHours { get; set; }
    public double CenterHours { get; set; }
    public double TotalHours => DeliveryHours + GroundHours + TowerHours + TraconHours + CenterHours;
}