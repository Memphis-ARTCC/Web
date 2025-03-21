﻿using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class Feedback
{
    public int Id { get; set; }
    public int Cid { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required User Controller { get; set; }
    public required string ControllerCallsign { get; set; }
    public required string Description { get; set; }
    public FeedbackLevel Level { get; set; }
    public string? Reply { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.PENDING;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}

public class FeedbackPayload
{
    public int ControllerId { get; set; }
    public required string ControllerCallsign { get; set; }
    public required string Description { get; set; }
    public FeedbackLevel Level { get; set; }
    public string? Reply { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.PENDING;
}