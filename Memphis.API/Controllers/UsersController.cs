using FluentValidation;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    public readonly IValidator<UserPayload> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<UserPayload> validator, ISentryClient sentryHub, ILogger<UsersController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet("Roster")]
    [Authorize]
    [ProducesResponseType(typeof(Response<IList<RosterUserDto>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<RosterUserDto>>>> GetRoster()
    {
        try
        {
            var users = await _context.Users
                .Include(x => x.Roles)
                .Include(x => x.Ground)
                .Include(x => x.Tower)
                .Include(x => x.Radar)
                .Include(x => x.Center)
                .Where(x => x.Status != Shared.Enums.UserStatus.REMOVED)
                .OrderBy(x => x.LastName)
                .ToListAsync();
            return Ok(new Response<IList<RosterUserDto>>
            {
                StatusCode = 200,
                Message = $"Got {users.Count} users",
                Data = RosterUserDto.ParseMany(users)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetRoster error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
