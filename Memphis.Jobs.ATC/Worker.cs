using Discord.Webhook;
using Memphis.Shared.Data;
using Memphis.Shared.Datafeed;
using Memphis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Memphis.Jobs.ATC
{
    public class Worker : BackgroundService
    {
        private readonly DatabaseContext _context;
        private readonly IDatabase _redis;
        private readonly DiscordWebhookClient _onlineWebhook;
        private readonly DiscordWebhookClient _staffWebhook;
        private readonly IList<string> _nonMemberControllers;
        private readonly ILogger<Worker> _logger;

        public Worker(DatabaseContext context, IDatabase redis, ILogger<Worker> logger)
        {
            _context = context;
            _redis = redis;
            _onlineWebhook = new DiscordWebhookClient(Environment.GetEnvironmentVariable("ONLINE_DISCORD_WEBHOOK") ??
                throw new ArgumentNullException("ONLINE_DISCORD_WEBHOOK env variable not found"));
            _staffWebhook = new DiscordWebhookClient(Environment.GetEnvironmentVariable("STAFF_DISCORD_WEBHOOK") ??
                throw new ArgumentNullException("STAFF_DISCORD_WEBHOOK env variable not found"));
            _nonMemberControllers = new List<string>();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait 1 second before starting
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ATC job running at: {time}", DateTimeOffset.UtcNow);

                var added = 0;
                var updated = 0;
                var removed = 0;

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

                var facilityIdentifiers = await _context.Facilities.Select(x => x.Identifier).ToListAsync();
                var memphisAtc = new List<Controller>();
                foreach (var entry in facilityIdentifiers)
                {
                    memphisAtc.AddRange(datafeed.Controllers.Where(x => x.Callsign.StartsWith(entry)));
                }

                // Update non artcc members list
                foreach (var entry in _nonMemberControllers)
                {
                    if (!memphisAtc.Any(x => x.Callsign.Equals(entry, StringComparison.OrdinalIgnoreCase)))
                    {
                        _nonMemberControllers.Remove(entry);
                    }
                }

                // Handle updating existing sessions and adding new ones
                foreach (var entry in memphisAtc)
                {
                    var existingSession = await _context.Sessions.Include(x => x.User)
                        .Where(x => x.User.Id == entry.Cid)
                        .Where(x => x.Callsign == entry.Callsign)
                        .Where(x => x.Start == entry.LogonTime)
                        .Where(x => x.Duration == TimeSpan.Zero)
                        .FirstOrDefaultAsync();
                    if (existingSession == null)
                    {
                        var user = await _context.Users.FindAsync(entry.Cid);
                        if (user == null)
                        {
                            _logger.LogError("Failed to find user {UserId}", entry.Cid);
                            await _staffWebhook.SendMessageAsync(
                                $":alert: **{entry.Name}** is online controlling **{entry.Callsign}** but is not a member! :alert:", false
                            );
                            _nonMemberControllers.Add(entry.Callsign);
                            continue;
                        }

                        await _context.Sessions.AddAsync(new Session
                        {
                            User = user,
                            Name = entry.Name,
                            Callsign = entry.Callsign,
                            Frequency = entry.Frequency,
                            Start = entry.LogonTime,
                            End = DateTimeOffset.UtcNow
                        });
                        await _onlineWebhook.SendMessageAsync($":white_check_mark: **{entry.Name}** is online controlling **{entry.Callsign}**", false);
                        added++;
                    }
                    else
                    {
                        existingSession.End = DateTimeOffset.UtcNow;
                        updated++;
                    }
                    await _context.SaveChangesAsync();
                }

                // Handle any sessions to remove
                var onlineAtc = await _context.Sessions.Include(x => x.User).Where(x => x.Duration == TimeSpan.Zero).ToListAsync();
                foreach (var entry in onlineAtc)
                {
                    if (!memphisAtc.Any(x => x.Callsign == entry.Callsign && x.Cid == entry.User.Id && x.LogonTime == entry.Start))
                    {
                        if ((DateTimeOffset.UtcNow - entry.End).TotalSeconds < 45)
                        {
                            // Only end a session if it has been offline for 45 seconds (3 datafeed refreshes)
                            continue;
                        }

                        entry.Duration = entry.End - entry.Start;
                        _context.Sessions.Update(entry);

                        if (!await _context.Hours.AnyAsync(x => x.User == entry.User && x.Year == DateTime.UtcNow.Year && x.Month == DateTime.UtcNow.Month))
                        {
                            await _context.Hours.AddAsync(new Hours
                            {
                                User = entry.User,
                                Year = DateTime.UtcNow.Year,
                                Month = DateTime.UtcNow.Month,
                                DeliveryHours = 0,
                                GroundHours = 0,
                                TowerHours = 0,
                                TraconHours = 0,
                                CenterHours = 0
                            });
                            await _context.SaveChangesAsync();
                        }
                        var hours = await _context.Hours
                            .Where(x => x.User == entry.User)
                            .Where(x => x.Year == DateTime.UtcNow.Year)
                            .Where(x => x.Year == DateTime.UtcNow.Year)
                            .FirstOrDefaultAsync();
                        if (hours == null)
                        {
                            _logger.LogError("Failed to find hours for user {UserId}", entry.User.Id);
                            continue;
                        }

                        if (entry.Callsign.EndsWith("DEL"))
                        {
                            hours.DeliveryHours += Math.Round(entry.Duration.TotalHours, 2);
                        }
                        else if (entry.Callsign.EndsWith("GND"))
                        {
                            hours.GroundHours += Math.Round(entry.Duration.TotalHours, 2);
                        }
                        else if (entry.Callsign.EndsWith("TWR"))
                        {
                            hours.TowerHours += Math.Round(entry.Duration.TotalHours, 2);
                        }
                        else if (entry.Callsign.EndsWith("APP") || entry.Callsign.EndsWith("DEP"))
                        {
                            hours.TraconHours += Math.Round(entry.Duration.TotalHours, 2);
                        }
                        else if (entry.Callsign.EndsWith("CTR"))
                        {
                            hours.CenterHours += Math.Round(entry.Duration.TotalHours, 2);
                        }
                        await _context.SaveChangesAsync();
                        removed++;
                        await _onlineWebhook.SendMessageAsync($":x: **{entry.Callsign}** is now offline, **{entry.Name}** controlled for **{entry.Duration.TotalHours}**", false);
                    }
                }

                var onlineControllers = await _context.OnlineControllers.ToListAsync();
                foreach (var entry in onlineControllers)
                {
                    _context.OnlineControllers.Remove(entry);
                    await _context.SaveChangesAsync();
                }
                var onlineSessions = await _context.Sessions.Include(x => x.User).Where(x => x.Duration == TimeSpan.Zero).ToListAsync();
                foreach (var entry in onlineSessions)
                {
                    await _context.OnlineControllers.AddAsync(new OnlineController
                    {
                        Cid = entry.User.Id,
                        Rating = entry.User.Rating,
                        Name = entry.Name,
                        Callsign = entry.Callsign,
                        Frequency = entry.Frequency,
                        Duration = (DateTimeOffset.UtcNow - entry.Start).ToString("g"),
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Added {added} sessions", added);
                _logger.LogInformation("Updated {updated} sessions", updated);
                _logger.LogInformation("Removed {removed} sessions", removed);
                _logger.LogInformation("ATC job completed at: {time}", DateTimeOffset.UtcNow);
                var delay = int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "10000");
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
