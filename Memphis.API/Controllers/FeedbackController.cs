using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
using Memphis.Shared.Enums;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Constants = Memphis.Shared.Utils.Constants;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<FeedbackDto> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<FeedbackDto> validator, ISentryClient sentryHub, ILogger<FeedbackController> logger)
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
    [ProducesResponseType(typeof(Response<Feedback>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Feedback>>> CreateFeedback(FeedbackDto payload)
    {
        try
        {
            var validation = await _validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var controller = await _context.Users.FindAsync(payload.ControllerId);
            if (controller == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"User '{payload.ControllerId}' not found"
                });
            }

            var result = await _context.Feedback.AddAsync(new Feedback
            {
                Cid = Request.HttpContext.GetCid() ?? 0,
                Name = Request.HttpContext.GetName() ?? string.Empty,
                Email = Request.HttpContext.GetEmail() ?? string.Empty,
                Controller = controller,
                ControllerCallsign = payload.ControllerCallsign,
                Description = payload.Description,
                Level = payload.Level
            });
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created feedback '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<Feedback>
            {
                StatusCode = 201,
                Message = $"Created feedback '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [Authorize(Roles = Constants.CanFeedback)]
    [ProducesResponseType(typeof(Response<Feedback>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Feedback>>>> GetAllFeedback(
        FeedbackStatus status = FeedbackStatus.PENDING, int page = 1, int size = 10)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
            {
                return StatusCode(401);
            }

            var result = await _context.Feedback.OrderBy(x => x.Created)
                .Where(x => x.Status == status).Skip((page - 1) * size).Take(size).ToListAsync();

            return Ok(new Response<IList<Feedback>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} feedback",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAllFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{feedbackId:int}")]
    [Authorize(Roles = Constants.CanFeedback)]
    [ProducesResponseType(typeof(Response<Feedback>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Feedback>>> GetFeedback(int feedbackId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
            {
                return StatusCode(401);
            }

            var result = await _context.Feedback.FindAsync(feedbackId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Feedback '{feedbackId}' not found",
                });
            }

            return Ok(new Response<Feedback>
            {
                StatusCode = 200,
                Message = $"Got feedback '{result.Id}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("own")]
    [Authorize]
    [ProducesResponseType(typeof(Response<Feedback>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<ResponsePaging<IList<Feedback>>>> GetOwnFeedback(int page = 1, int size = 10)
    {
        try
        {
            var controller = await Request.HttpContext.GetUser(_context);
            if (controller == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found",
                });
            }

            var result = await _context.Feedback
                .Where(x => x.Controller == controller && x.Status == FeedbackStatus.APPROVED)
                .Skip((page - 1) * size).Take(size).ToListAsync();
            var totalCount = await _context.Feedback
                .Where(x => x.Controller == controller && x.Status == FeedbackStatus.APPROVED)
                .CountAsync();
            return Ok(new ResponsePaging<IList<Feedback>>
            {
                StatusCode = 200,
                TotalCount = totalCount,
                ResultCount = result.Count,
                Message = $"Got {result.Count} feedback",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetOwnFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CanFeedback)]
    [ProducesResponseType(typeof(Response<Feedback>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Feedback>>> UpdateFeedback(FeedbackDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
            {
                return StatusCode(401);
            }

            var validation = await _validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var feedback = await _context.Feedback.FindAsync(payload.Id);
            if (feedback == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Feedback '{payload.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(feedback);
            feedback.Reply = payload.Reply;
            feedback.Status = payload.Status;
            feedback.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(feedback);

            await _loggingService.AddWebsiteLog(Request, $"Updated feedback '{feedback.Id}'", oldData, newData);

            // todo: send email to person with reply-to of staff member who processed feedback if reply was not null
            // todo: send email to controller if feedback was approved

            return Ok(new Response<Feedback>
            {
                StatusCode = 200,
                Message = $"Updated feedback '{feedback.Id}'",
                Data = feedback
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{feedbackId:int}")]
    [Authorize(Roles = Constants.CanFeedback)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteFeedback(int feedbackId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
            {
                return StatusCode(401);
            }

            var result = await _context.Feedback.FindAsync(feedbackId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Feedback '{feedbackId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(result);
            _context.Feedback.Remove(result);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted feedback '{feedbackId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted feedback '{feedbackId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}