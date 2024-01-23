using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentry;
using Constants = Memphis.Shared.Utils.Constants;
using Session = Memphis.Shared.Models.Session;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class SessionsController(DatabaseContext context, RedisService redisService, ISentryClient sentryHub,
        ILogger<SessionsController> logger)
    : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<IList<Session>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<ResponsePaging<IList<Session>>>> GetSessions(int page, int size)
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

            var sessions = await context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .OrderBy(x => x.Start)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();
            var totalCount = await context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .CountAsync();

            return Ok(new ResponsePaging<IList<Session>>
            {
                StatusCode = 200,
                ResultCount = sessions.Count,
                TotalCount = totalCount,
                Message = $"Got {sessions.Count} sessions",
                Data = sessions
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetSessions error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{userId:int}")]
    [Authorize(Roles = Constants.AllStaff)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<IList<Session>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<ResponsePaging<IList<Session>>>> GetUserSessions(int userId, int page, int size)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var sessions = await context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .OrderBy(x => x.Start)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();
            var totalCount = await context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .CountAsync();

            return Ok(new ResponsePaging<IList<Session>>
            {
                StatusCode = 200,
                ResultCount = sessions.Count,
                TotalCount = totalCount,
                Message = $"Got {sessions.Count} sessions",
                Data = sessions
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetSessions error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}