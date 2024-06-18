using Memphis.Shared.Status;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Net.Http.Json;

namespace Memphis.Jobs.Datafeed;

public class Worker : BackgroundService
{
    private readonly IDatabase _redis;
    private readonly ILogger<Worker> _logger;

    public Worker(IDatabase redis, ILogger<Worker> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 1 second before starting
        await Task.Delay(1000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Datafeed job running at: {time}", DateTimeOffset.UtcNow);

            using var client = new HttpClient();
            var statusUrl = Environment.GetEnvironmentVariable("STATUS_URL") ??
                throw new ArgumentNullException("STATUS_URL env variable not found");
            var response = await client.GetFromJsonAsync<Status>(statusUrl, stoppingToken);

            if (response is null)
            {
                _logger.LogError("Failed to fetch status data");
                await Task.Delay(10000, stoppingToken);
                continue;
            }

            var dataUrl = response.Data.V3.First();
            var data = await client.GetFromJsonAsync<Shared.Datafeed.Datafeed>(dataUrl, stoppingToken);

            if (data is null)
            {
                _logger.LogError("Failed to fetch datafeed data");
                await Task.Delay(10000, stoppingToken);
                continue;
            }

            var dataJson = JsonConvert.SerializeObject(data);
            await _redis.StringSetAsync("datafeed", dataJson, TimeSpan.FromSeconds(20));
            _logger.LogInformation("Datafeed data saved to redis");

            var delay = int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "10000");
            await Task.Delay(delay, stoppingToken);
        }
    }
}
