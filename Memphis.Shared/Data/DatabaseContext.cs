﻿using Memphis.Shared.Models;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed roles
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 1,
            Name = "Air Traffic Manager",
            NameShort = "ATM",
            Email = "atm@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 2,
            Name = "Deputy Air Traffic Manager",
            NameShort = "DATM",
            Email = "datm@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 3,
            Name = "Training Administrator",
            NameShort = "TA",
            Email = "ta@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 4,
            Name = "Assistant Training Administrator",
            NameShort = "ATA",
            Email = "ata@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 5,
            Name = "Webmaster",
            NameShort = "WM",
            Email = "wm@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 6,
            Name = "Assistant Webmaster",
            NameShort = "AWM",
            Email = "awm@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 7,
            Name = "Events Coordinator",
            NameShort = "EC",
            Email = "ec@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 8,
            Name = "Assistant Events Coordinator",
            NameShort = "AEC",
            Email = "aec@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 9,
            Name = "Facility Engineer",
            NameShort = "FE",
            Email = "fe@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 10,
            Name = "Assistant Facility Engineer",
            NameShort = "AFE",
            Email = "afe@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 11,
            Name = "Web Team",
            NameShort = "WEB",
            Email = "web@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 12,
            Name = "Events Team",
            NameShort = "EVENTS",
            Email = "events@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 13,
            Name = "Facilities Team",
            NameShort = "FACILITIES",
            Email = "facilities@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 14,
            Name = "Social Media Team",
            NameShort = "SOCIAL",
            Email = "socialmedia@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 15,
            Name = "Instructor",
            NameShort = "INS",
            Email = "instructors@memphisartcc.com"
        });
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 16,
            Name = "Mentor",
            NameShort = "MTR",
            Email = "mentors@memphisartcc.com"
        });
    }
}