using FluentValidation;
using Memphis.API.Data;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace Memphis.API.Controllers;

public class OtsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<Ots> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<OtsController> _logger;

    public OtsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<Ots> validator, IHub sentryHub, ILogger<OtsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }
}