using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sentry;
using Constants = Memphis.Shared.Utils.Constants;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class SettingsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        ISentryClient sentryHub, ILogger<SettingsController> logger)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<Settings>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Settings>>> GetSettings()
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
                return StatusCode(401);

            var settings = await context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Settings not found"
                });
            }

            return Ok(new Response<Settings>
            {
                StatusCode = 200,
                Message = "Got settings",
                Data = settings
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetSettings error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }


    [HttpPut]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<Settings>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Settings>>> UpdateSettings(Settings payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
                return StatusCode(401);

            var settings = await context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Settings not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(settings);
            settings.VisitingOpen = payload.VisitingOpen;
            settings.RequiredHours = payload.RequiredHours;
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(settings);

            await loggingService.AddWebsiteLog(Request, "Updated settings", oldData, newData);

            return Ok(new Response<Settings>
            {
                StatusCode = 200,
                Message = "Updated settings",
                Data = settings
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetSettings error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}