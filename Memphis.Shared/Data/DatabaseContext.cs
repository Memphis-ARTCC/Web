using Memphis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using File = Memphis.Shared.Models.File;

namespace Memphis.Shared.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public required DbSet<Airport> Airports { get; set; }
    public required DbSet<Comment> Comments { get; set; }
    public required DbSet<EmailLog> EmailLogs { get; set; }
    public required DbSet<Event> Events { get; set; }
    public required DbSet<EventPosition> EventPositions { get; set; }
    public required DbSet<EventRegistration> EventRegistrations { get; set; }
    public required DbSet<Exam> Exams { get; set; }
    public required DbSet<ExamRequest> ExamRequests { get; set; }
    public required DbSet<Facility> Facilities { get; set; }
    public required DbSet<Feedback> Feedback { get; set; }
    public required DbSet<File> Files { get; set; }
    public required DbSet<Hours> Hours { get; set; }
    public required DbSet<News> News { get; set; }
    public required DbSet<Notification> Notifications { get; set; }
    public required DbSet<OnlineController> OnlineControllers { get; set; }
    public required DbSet<Ots> Ots { get; set; }
    public required DbSet<Role> Roles { get; set; }
    public required DbSet<Session> Sessions { get; set; }
    public required DbSet<Settings> Settings { get; set; }
    public required DbSet<SoloCert> SoloCerts { get; set; }
    public required DbSet<StaffingRequest> StaffingRequests { get; set; }
    public required DbSet<TrainingMilestone> TrainingMilestones { get; set; }
    public required DbSet<TrainingScheduleEntry> TrainingScheduleEntries { get; set; }
    public required DbSet<TrainingSchedule> TrainingSchedules { get; set; }
    public required DbSet<TrainingTicket> TrainingTickets { get; set; }
    public required DbSet<TrainingType> TrainingTypes { get; set; }
    public required DbSet<User> Users { get; set; }
    public required DbSet<VisitingApplication> VisitingApplications { get; set; }
    public required DbSet<WebsiteLog> WebsiteLogs { get; set; }
}