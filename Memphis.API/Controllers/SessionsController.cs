using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Constants = Memphis.Shared.Utils.Constants;
using Session = Memphis.Shared.Models.Session;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(DatabaseContext context, RedisService redisService, ISentryClient sentryHub,
        ILogger<SessionsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

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
            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .OrderBy(x => x.Start)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();
            var totalCount = await _context.Sessions
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
            _logger.LogError("GetSessions error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{userId:int}")]
    [Authorize(Roles = Constants.FullStaff)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<IList<Session>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<ResponsePaging<IList<Session>>>> GetUserSessions(int userId, int page, int size)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.FullStaffList))
            {
                return StatusCode(401);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.User == user)
                .OrderBy(x => x.Start)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();
            var totalCount = await _context.Sessions
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
            _logger.LogError("GetSessions error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}