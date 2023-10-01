using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sentry;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<Feedback> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<Feedback> validator, IHub sentryHub, ILogger<FeedbackController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Response<Feedback>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Feedback>>> CreateFeedback(Feedback data)
    {
        try
        {
            var validation = await _validator.ValidateAsync(data);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var user = await _context.Users.FindAsync(data.ControllerId);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"User '{data.ControllerId}' not found"
                });
            }

            data.ControllerName = $"{user.FirstName} {user.LastName}";

            var result = await _context.Feedback.AddAsync(data);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created feedback '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<Feedback>
            {
                StatusCode = 200,
                Message = $"Created feedback '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
