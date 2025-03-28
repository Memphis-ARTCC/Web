using Memphis.Shared.Data;
using Memphis.Shared.Dtos.Avwx;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Memphis.Jobs.Airports
{
    public class Worker : BackgroundService
    {
        private readonly DatabaseContext _context;
        private readonly IDatabase _redis;
        private readonly ILogger<Worker> _logger;

        public Worker(DatabaseContext context, IDatabase redis, ILogger<Worker> logger)
        {
            _context = context;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait 1 second before starting
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var redisDatafeed = await _redis.StringGetAsync("datafeed");
                if (!redisDatafeed.HasValue)
                {
                    _logger.LogError("Failed to fetch datafeed data");
                    await Task.Delay(int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "10000"), stoppingToken);
                    continue;
                }
                var datafeed = JsonConvert.DeserializeObject<Shared.Datafeed.Datafeed>(redisDatafeed!);
                if (datafeed is null)
                {
                    _logger.LogError("Failed to deserialize datafeed data");
                    await Task.Delay(int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "10000"), stoppingToken);
                    continue;
                }

                var client = new HttpClient();
                var apiKey = Environment.GetEnvironmentVariable("AVWX_API_KEY") ??
                    throw new ArgumentNullException("AVWX_API_KEY env var not found");

                var airports = await _context.Airports.ToListAsync();
                foreach (var airport in airports)
                {
                    var url = $"https://avwx.rest/api/metar/{airport.Icao}?token={apiKey}";
                    var response = await client.GetAsync(url);
                    _logger.LogInformation(response.Content.ReadAsStringAsync().Result);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = JsonConvert.DeserializeObject<AvwxError>(response.Content.ReadAsStringAsync().Result);
                        _logger.LogError("Failed to fetch data for {Icao}\nError: {Error}", airport.Icao, errorBody?.Error);
                        continue;
                    }
                    var body = JsonConvert.DeserializeObject<AvwxResponse>(response.Content.ReadAsStringAsync().Result);
                    if (body is null)
                    {
                        _logger.LogError("Failed to deserialize data for {Icao}", airport.Icao);
                        continue;
                    }

                    var arrivals = datafeed.Pilots.Where(x => x.FlightPlan?.Arrival.Equals(airport.Icao, StringComparison.OrdinalIgnoreCase) ?? false).Count();
                    var departures = datafeed.Pilots.Where(x => x.FlightPlan?.Departure.Equals(airport.Icao, StringComparison.OrdinalIgnoreCase) ?? false).Count();

                    airport.Arrivals = arrivals;
                    airport.Departures = departures;
                    airport.FlightRules = body.FlightRules;
                    airport.Visibility = body.Visibility.Repr;
                    var winds = $"{body.WindDirection.Repr}\u00b0 at {body.WindSpeed.Repr}";
                    if (body.WindGust != null)
                    {
                        winds += $" Gusting {body.WindGust.Repr}";
                    }
                    airport.Wind = winds;
                    airport.Altimeter = body.Altimeter.Repr.Replace("A", "").Insert(2, ".");
                    airport.Temperature = body.Temperature.Repr;
                    airport.MetarRaw = body.Raw;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated {Icao}", airport.Icao);
                }

                var delay = int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "10000");
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
