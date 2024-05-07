using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly LoggingService _loggingService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(DatabaseContext context, LoggingService loggingService, ISentryClient sentryHub,
        ILogger<NotificationsController> logger)
    {
        _context = context;
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
    public async Task<ActionResult<Response<IList<NotificationDto>>>> GetNotifications(int page = 1, int size = 5)
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

            var notifications = await _context.Notifications
                .Where(x => x.User == user)
                .OrderBy(x => x.Timestamp)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();

            return Ok(new ResponsePaging<IList<NotificationDto>>
            {
                StatusCode = 200,
                Message = $"Got {notifications.Count} notifications",
                Data = NotificationDto.ParseMany(notifications)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetNotifications error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("Read/{notificationId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(Response<NotificationDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<NotificationDto>>> MarkNotificationAsRead(int notificationId)
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

            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Notification not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(notification);
            notification.Read = true;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(notification);
            await _loggingService.AddWebsiteLog(Request, "Marked notification as read", oldData, newData);

            return Ok(new Response<NotificationDto>
            {
                StatusCode = 200,
                Message = $"Marked notification '{notificationId}' as read",
                Data = NotificationDto.Parse(notification)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("MarkNotificationAsRead error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}