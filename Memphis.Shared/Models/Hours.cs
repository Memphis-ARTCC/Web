using Microsoft.EntityFrameworkCore;

namespace Memphis.Shared.Models;

[Index(nameof(UserId))]
[Index(nameof(Month))]
[Index(nameof(Year))]
public class Hours
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public float DeliveryHours { get; set; }
    public float GroundHours { get; set; }
    public float TowerHours { get; set; }
    public float ApproachHours { get; set; }
    public float CenterHours { get; set; }
}
