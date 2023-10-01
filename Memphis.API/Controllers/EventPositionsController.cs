using FluentValidation;
using FluentValidation.Results;
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
public class EventPositionsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly S3Service _s3Service;
    private readonly IValidator<EventPosition> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<EventPositionsController> _logger;

    public EventPositionsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        S3Service s3Service, IValidator<EventPosition> validator, IHub sentryHub, ILogger<EventPositionsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _s3Service = s3Service;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(typeof(Response<EventPosition>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventPosition>>> CreateEventPosition(EventPosition data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

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

            if (!await _context.Events.AnyAsync(x => x.Id == data.EventId))
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{data.EventId}' not found"
                });
            }

            var result = await _context.EventPositions.AddAsync(data);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created event position '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<EventPosition>
            {
                StatusCode = 200,
                Message = $"Created event position '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateEventPosition error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{eventId:int}")]
    [ProducesResponseType(typeof(Response<IList<EventPosition>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<EventPosition>>>> GetEventPositions(int eventId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.ALL_STAFF_LIST))
            {
                if (!await _context.Events.AnyAsync(x => x.Id == eventId && x.IsOpen))
                {
                    return NotFound(new Response<string?>
                    {
                        StatusCode = 404,
                        Message = $"Event '{eventId}' not found"
                    });
                }
                var result = await _context.EventPositions.Where(x => x.EventId == eventId).ToListAsync();
                return Ok(new Response<IList<EventPosition>>
                {
                    StatusCode = 200,
                    Message = $"Got '{result.Count}' event positions",
                    Data = result
                });
            }
            else
            {
                if (!await _context.Events.AnyAsync(x => x.Id == eventId))
                {
                    return NotFound(new Response<string?>
                    {
                        StatusCode = 404,
                        Message = $"Event '{eventId}' not found"
                    });
                }
                var result = await _context.EventPositions.Where(x => x.EventId == eventId).ToListAsync();
                return Ok(new Response<IList<EventPosition>>
                {
                    StatusCode = 200,
                    Message = $"Got '{result.Count}' event positions",
                    Data = result
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEventPositions error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("Positions/{eventPositionId:int}")]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteEventPosition(int eventPositionId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

            var eventPosition = await _context.EventPositions.FindAsync(eventPositionId);
            if (eventPosition == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event position '{eventPositionId}' not found"
                });
            }
            var eventRegistrations = await _context.EventRegistrations.Where(x => x.EventPositionId == eventPositionId).ToListAsync();

            // Delete registrations that are for the given position
            foreach (var entry in eventRegistrations)
            {
                var registrationOldData = JsonConvert.SerializeObject(entry);

                // todo: send email that position was removed
                _context.EventRegistrations.Remove(entry);
                await _context.SaveChangesAsync();
                await _loggingService.AddWebsiteLog(Request, $"Deleted event position '{entry.Id}'", registrationOldData, string.Empty);
            }

            // Now delete the position
            var oldData = JsonConvert.SerializeObject(eventPosition);
            _context.EventPositions.Remove(eventPosition);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted event position '{eventPositionId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted event position '{eventPositionId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEventPosition error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
