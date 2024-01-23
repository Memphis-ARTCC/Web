#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace Memphis.Shared.Models;

[Index(nameof(Name))]
[Index(nameof(Icao))]
public class Airport
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Icao { get; set; }
    public int Arrivals { get; set; }
    public int Departures { get; set; }
    public string? Wind { get; set; }
    public string? Altimeter { get; set; }
    public string? Temperature { get; set; }
    public string? MetarRaw { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}