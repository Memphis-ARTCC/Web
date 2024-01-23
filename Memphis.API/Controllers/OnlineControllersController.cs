using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentry;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class OnlineControllersController(DatabaseContext context, ISentryClient sentryHub,
        ILogger<OnlineControllersController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<OnlineController>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<OnlineController>>>> GetOnlineControllers()
    {
        try
        {
            var result = await context.OnlineControllers.OrderBy(x => x.Name).ToListAsync();
            return Ok(new Response<IList<OnlineController>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} online controllers",
                Data = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetOnlineControllers error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}