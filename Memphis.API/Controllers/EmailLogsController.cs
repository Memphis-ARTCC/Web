using FluentValidation;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Constants = Memphis.Shared.Utils.Constants;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EmailLogsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<EmailLogsController> _logger;

    public EmailLogsController(DatabaseContext context, RedisService redisService, ISentryClient sentryHub, ILogger<EmailLogsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Constants.CanEmailLogs)]
    [ProducesResponseType(typeof(Response<IList<EmailLog>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<ResponsePaging<IList<EmailLog>>>> GetEmailLogs(int page, int size, string? to = null)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanEmailLogsList))
            {
                return StatusCode(401);
            }

            var raw = _context.EmailLogs.OrderBy(x => x.Timestamp);
            if (to != null)
                raw.Where(x => x.To.ToLower() == to.ToLower());
            var result = await raw.Skip((page - 1) * size).Take(size).ToListAsync();
            var totalCount = await raw.CountAsync();

            return Ok(new ResponsePaging<IList<EmailLog>>
            {
                StatusCode = 200,
                TotalCount = totalCount,
                ResultCount = result.Count,
                Message = $"Got {result.Count} email logs",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEmailLogs error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}