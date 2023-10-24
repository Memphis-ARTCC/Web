using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentry;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IHub _sentryHub;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IHub sentryHub, ILogger<NotificationsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(Response<IList<NotificationDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<NotificationDto>>>> GetNotifications()
    {
        try
        {
            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var notifications = await _context.Notifications.Where(x => x.User == user).OrderBy(x => x.Timestamp)
                .ToListAsync();
            var result = new List<NotificationDto>();
            foreach (var entry in notifications)
            {
                result.Add(new NotificationDto
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Link = entry.Link,
                    Read = entry.Read,
                    Timestamp = entry.Timestamp,
                });
            }

            return Ok(new Response<IList<NotificationDto>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} notifications",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetNotifications error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}