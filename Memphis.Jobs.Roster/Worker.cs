using Discord;
using Discord.Webhook;
using Memphis.Shared.Data;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Memphis.Shared.Vatusa;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace Memphis.Jobs.Roster
{
    public class Worker : BackgroundService
    {
        private readonly DatabaseContext _context;
        private readonly DiscordWebhookClient _rosterWebhook;
        private readonly ILogger<Worker> _logger;

        public Worker(DatabaseContext context, ILogger<Worker> logger)
        {
            _context = context;
            _rosterWebhook = new DiscordWebhookClient(Environment.GetEnvironmentVariable("ROSTER_DISCORD_WEBHOOK") ??
                throw new ArgumentNullException("ROSTER_DISCORD_WEBHOOK env variable not found"));
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait 1 second before starting
                await Task.Delay(1000, stoppingToken);

                _logger.LogInformation("Roster job running at: {time}", DateTimeOffset.UtcNow);

                using var client = new HttpClient();
                var apiKey = Environment.GetEnvironmentVariable("VATUSA_API_KEY") ??
                    throw new ArgumentNullException("VATUSA_API_KEY env variable not found");
                var facility = Environment.GetEnvironmentVariable("VATUSA_FACILITY") ??
                    throw new ArgumentNullException("VATUSA_FACILITY env variable not found");
                var response = await client.GetFromJsonAsync<RosterResponseDto>($"https://api.vatusa.net/facility/{facility}/roster/both?apikey={apiKey}",
                    stoppingToken);
                if (response == null)
                {
                    _logger.LogError("Failed to retrieve roster data from VATUSA API");
                    return;
                }

                var apiUsers = response.Data;
                var toRemove = new List<User>();

                _logger.LogInformation("Updating existing users...");
                var updatedCount = 0;
                var existingUsers = await _context.Users.Include(x => x.Roles).ToListAsync();
                foreach (User user in existingUsers)
                {
                    var wasUpdated = false;

                    var apiUser = apiUsers.FirstOrDefault(x => x.Cid == user.Id);
                    if (apiUser == null)
                    {
                        toRemove.Add(user);
                        continue;
                    }

                    // User switched from home controller to a visitor
                    if (!apiUser.Facility.Equals(facility, StringComparison.OrdinalIgnoreCase) && !user.Visitor)
                    {
                        user.Visitor = true;
                        user.VisitorFrom = apiUser.Facility;
                    }

                    // User switched from visitor to home controller
                    if (apiUser.Facility.Equals(facility, StringComparison.OrdinalIgnoreCase) && user.Visitor)
                    {
                        user.Visitor = false;
                        user.VisitorFrom = null;
                    }

                    if (user.FirstName != apiUser.FirstName)
                    {
                        user.FirstName = apiUser.FirstName;
                        wasUpdated = true;
                    }

                    if (user.LastName != apiUser.LastName)
                    {
                        user.LastName = apiUser.LastName;
                        wasUpdated = true;
                    }

                    if (user.Email != apiUser.Email)
                    {
                        user.Email = apiUser.Email;
                        wasUpdated = true;
                    }

                    if (user.Rating != apiUser.Rating)
                    {
                        user.Rating = apiUser.Rating;
                        wasUpdated = true;
                    }

                    foreach (var role in apiUser.Roles)
                    {
                        user.Roles ??= new List<Role>();
                        var existingRole = user.Roles?.FirstOrDefault(x => x.NameShort == role.Role);
                        if (existingRole == null)
                        {
                            var zmeRole = await _context.Roles.FirstOrDefaultAsync(x => x.NameShort == role.Role);
                            if (zmeRole != null)
                            {
                                user.Roles?.Add(zmeRole);
                                wasUpdated = true;
                            }
                            else
                            {
                                _logger.LogWarning("Role {Role} not found in database, skipping", role.Role);
                            }
                        }
                    }

                    if (wasUpdated)
                    {
                        _logger.LogInformation("{User} was updated", user.Id);
                        var embed = new EmbedBuilder
                        {
                            Title = $"{user.FirstName} {user.LastName} was updated",
                            Fields =
                            {
                                new EmbedFieldBuilder
                                {
                                    Name = "CID",
                                    Value = user.Id,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Email",
                                    Value = user.Email,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Rating",
                                    Value = Helpers.GetRatingName(user.Rating),
                                    IsInline = false
                                }
                            },
                            Color = Color.Orange,
                            Timestamp = DateTimeOffset.UtcNow
                        };
                        await _rosterWebhook.SendMessageAsync("", false, [embed.Build()]);
                        user.Updated = DateTime.UtcNow;
                        updatedCount++;
                        await _context.SaveChangesAsync();
                    }

                    apiUsers.Remove(apiUser);
                }
                _logger.LogInformation("Updated {Count} users", updatedCount);

                _logger.LogInformation("Adding new users...");
                var addedCount = 0;
                foreach (RosterDto user in apiUsers)
                {
                    await _context.Users.AddAsync(new User
                    {
                        Id = user.Cid,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Initials = await GetInitials(user.FirstName, user.LastName),
                        Email = user.Email,
                        Rating = user.Rating,
                        Joined = user.FacilityJoin,
                        Status = Shared.Enums.UserStatus.ACTIVE,
                        Visitor = !user.Facility.Equals(facility, StringComparison.OrdinalIgnoreCase),
                        VisitorFrom = !user.Facility.Equals(facility, StringComparison.OrdinalIgnoreCase) ? user.Facility : null,
                        Roles = new List<Role>(),
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    addedCount++;
                    var embed = new EmbedBuilder
                    {
                        Title = $"{user.FirstName} {user.LastName} was added",
                        Fields =
                            {
                                new EmbedFieldBuilder
                                {
                                    Name = "CID",
                                    Value = user.Cid,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Email",
                                    Value = user.Email,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Rating",
                                    Value = Helpers.GetRatingName(user.Rating),
                                    IsInline = false
                                }
                            },
                        Color = Color.Green,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    await _rosterWebhook.SendMessageAsync("", false, [embed.Build()]);
                }
                _logger.LogInformation("Added {Count} users", addedCount);

                _logger.LogInformation("Removing users...");
                var removedCount = 0;
                foreach (User user in toRemove)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    removedCount++;
                    var embed = new EmbedBuilder
                    {
                        Title = $"{user.FirstName} {user.LastName} was removed",
                        Fields =
                            {
                                new EmbedFieldBuilder
                                {
                                    Name = "CID",
                                    Value = user.Id,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Email",
                                    Value = user.Email,
                                    IsInline = false
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Rating",
                                    Value = Helpers.GetRatingName(user.Rating),
                                    IsInline = false
                                }
                            },
                        Color = Color.Red,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    await _rosterWebhook.SendMessageAsync("", false, [embed.Build()]);
                }
                _logger.LogInformation("Removed {Count} users", removedCount);

                _logger.LogInformation("Roster job completed");

                var delay = int.Parse(Environment.GetEnvironmentVariable("DELAY") ?? "3600000");
                await Task.Delay(delay, stoppingToken);
            }
        }

        protected async Task<string> GetInitials(string firstName, string lastName)
        {
            var initials = $"{firstName[0]}{lastName[0]}";

            var initialsExist = await _context.Users
                .Where(x => x.Initials.Equals(initials))
                .ToListAsync();

            if (!initialsExist.Any()) return initials;

            foreach (var letter in lastName)
            {
                initials = $"{firstName[0]}{letter.ToString().ToUpper()}";

                var exists = await _context.Users
                    .Where(x => x.Initials.Equals(initials))
                    .ToListAsync();

                if (!exists.Any()) return initials.ToUpper();
            }

            foreach (var letter in firstName)
            {
                initials = $"{letter.ToString().ToUpper()}{lastName[0]}";

                var exists = await _context.Users
                    .Where(x => x.Initials.Equals(initials))
                    .ToListAsync();

                if (!exists.Any()) return initials.ToUpper();
            }

            return string.Empty;
        }
    }
}
