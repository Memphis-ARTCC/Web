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
public class OnlineControllersController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly IHub _sentryHub;
    private readonly ILogger<OnlineControllersController> _logger;

    public OnlineControllersController(DatabaseContext context, IHub sentryHub,
        ILogger<OnlineControllersController> logger)
    {
        _context = context;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<OnlineController>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<OnlineController>>>> GetOnlineControllers()
    {
        try
        {
            var result = await _context.OnlineControllers.OrderBy(x => x.Name).ToListAsync();
            return Ok(new Response<IList<OnlineController>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} online controllers",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetOnlineControllers error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}