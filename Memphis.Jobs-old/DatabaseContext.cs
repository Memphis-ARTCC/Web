using Memphis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Memphis.Jobs;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public required DbSet<EmailLog> EmailLogs { get; set; }
    public required DbSet<Event> Events { get; set; }
    public required DbSet<EventPosition> EventPositions { get; set; }
    public required DbSet<EventRegistration> EventRegistrations { get; set; }
    public required DbSet<Hours> Hours { get; set; }
    public required DbSet<Notification> Notifications { get; set; }
    public required DbSet<OnlineController> OnlineControllers { get; set; }
    public required DbSet<Role> Roles { get; set; }
    public required DbSet<Session> Sessions { get; set; }
    public required DbSet<TrainingScheduleEntry> TrainingScheduleEntries { get; set; }
    public required DbSet<TrainingSchedule> TrainingSchedules { get; set; }
    public required DbSet<TrainingTicket> TrainingTickets { get; set; }
    public required DbSet<TrainingType> TrainingTypes { get; set; }
    public required DbSet<User> Users { get; set; }
    public required DbSet<WebsiteLog> WebsiteLogs { get; set; }
}
