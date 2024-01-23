using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sentry;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class NotificationsController(DatabaseContext context, LoggingService loggingService, ISentryClient sentryHub,
        ILogger<NotificationsController> logger)
    : ControllerBase
{
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
            var user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var notifications = await context.Notifications
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
            logger.LogError("GetNotifications error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var notification = await context.Notifications.FindAsync(notificationId);
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
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(notification);
            await loggingService.AddWebsiteLog(Request, "Marked notification as read", oldData, newData);

            return Ok(new Response<NotificationDto>
            {
                StatusCode = 200,
                Message = $"Marked notification '{notificationId}' as read",
                Data = NotificationDto.Parse(notification)
            });
        }
        catch (Exception ex)
        {
            logger.LogError("MarkNotificationAsRead error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}