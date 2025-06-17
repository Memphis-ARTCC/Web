using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Memphis.API.Controllers;

public class ProfileController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly LoggingService _loggingService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(DatabaseContext context, LoggingService loggingService, ISentryClient sentryHub, ILogger<ProfileController> logger)
    {
        _context = context;
        _loggingService = loggingService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(Response<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> UpdateDiscordId(string discordId)
    {
        try
        {
            var user = await HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found."
                });
            }

            var oldData = JsonConvert.SerializeObject(user);
            user.DiscordId = discordId;
            user.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(user);
            await _loggingService.AddWebsiteLog(Request, $"'{user.Id}' updated their discord ID", oldData, newData);

            return Ok(new Response<string>
            {
                StatusCode = 200,
                Message = "Discord ID updated successfully.",
                Data = user.DiscordId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateDiscordId error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
