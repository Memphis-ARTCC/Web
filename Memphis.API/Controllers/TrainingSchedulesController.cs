using FluentValidation;
using Memphis.API.Data;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TrainingSchedulesController(DatabaseContext context, RedisService redisService,
        LoggingService loggingService, IValidator<TrainingSchedule> validator, ISentryClient sentryHub,
        ILogger<TrainingSchedulesController> logger)
    : ControllerBase
{
}