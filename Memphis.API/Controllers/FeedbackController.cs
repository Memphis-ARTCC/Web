using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Enums;
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
public class FeedbackController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<FeedbackDto> validator, ISentryClient sentryHub, ILogger<FeedbackController> logger)
    : ControllerBase
{
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
            var validation = await validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var controller = await context.Users.FindAsync(payload.ControllerId);
            if (controller == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"User '{payload.ControllerId}' not found"
                });
            }

            var result = await context.Feedback.AddAsync(new Feedback
            {
                Cid = Request.HttpContext.GetCid() ?? 0,
                Name = Request.HttpContext.GetName() ?? string.Empty,
                Email = Request.HttpContext.GetEmail() ?? string.Empty,
                Controller = controller,
                ControllerCallsign = payload.ControllerCallsign,
                Description = payload.Description,
                Level = payload.Level
            });
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await loggingService.AddWebsiteLog(Request, $"Created feedback '{result.Entity.Id}'", string.Empty,
                newData);

            return StatusCode(201, new Response<Feedback>
            {
                StatusCode = 201,
                Message = $"Created feedback '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
                return StatusCode(401);

            var result = await context.Feedback.OrderBy(x => x.Created)
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
            logger.LogError("GetAllFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
                return StatusCode(401);

            var result = await context.Feedback.FindAsync(feedbackId);
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
            logger.LogError("GetFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var controller = await Request.HttpContext.GetUser(context);
            if (controller == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found",
                });
            }

            var result = await context.Feedback
                .Where(x => x.Controller == controller && x.Status == FeedbackStatus.APPROVED)
                .Skip((page - 1) * size).Take(size).ToListAsync();
            var totalCount = await context.Feedback
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
            logger.LogError("GetOwnFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
                return StatusCode(401);

            var validation = await validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var feedback = await context.Feedback.FindAsync(payload.Id);
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
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(feedback);

            await loggingService.AddWebsiteLog(Request, $"Updated feedback '{feedback.Id}'", oldData, newData);

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
            logger.LogError("UpdateFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFeedbackList))
                return StatusCode(401);

            var result = await context.Feedback.FindAsync(feedbackId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Feedback '{feedbackId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(result);
            context.Feedback.Remove(result);
            await context.SaveChangesAsync();

            await loggingService.AddWebsiteLog(Request, $"Deleted feedback '{feedbackId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted feedback '{feedbackId}'"
            });
        }
        catch (Exception ex)
        {
            logger.LogError("DeleteFeedback error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}