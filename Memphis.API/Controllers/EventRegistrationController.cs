using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
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
public class EventRegistrationController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly S3Service _s3Service;
    private readonly IValidator<EventRegistration> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<EventRegistrationController> _logger;

    public EventRegistrationController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        S3Service s3Service, IValidator<EventRegistration> validator, IHub sentryHub, ILogger<EventRegistrationController> logger)
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
    [Authorize(Roles = Constants.CAN_REGISTER_FOR_EVENTS)]
    [ProducesResponseType(typeof(Response<EventRegistration>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventRegistration>>> CreateEventRegistration(EventRegistration data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, new string[] { Constants.CAN_REGISTER_FOR_EVENTS }))
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

            var @event = await _context.Events.FindAsync(data.EventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{data.EventId}' not found"
                });
            }
            var position = await _context.EventPositions.FindAsync(data.EventPositionId);
            if (position == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event position '{data.EventPositionId}' not found"
                });
            }
            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var existingRegistrations = await _context.EventRegistrations
                .AnyAsync(x => x.EventId == data.EventId && x.UserId == user.Id);
            var failures = new List<ValidationFailure>();
            if (existingRegistrations)
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.EventId),
                    AttemptedValue = data.EventId,
                    ErrorMessage = $"User already has an event registration for event '{data.EventId}'",
                });
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = failures
                });
            }
            if (user.Rating < position.MinRating)
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(user.Rating),
                    AttemptedValue = user.Rating,
                    ErrorMessage = $"User rating is less than {position.MinRating}",
                });
            }
            if (data.Start < @event.Start.AddMinutes(-1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.Start),
                    AttemptedValue = data.Start,
                    ErrorMessage = $"Registration start '{data.Start:u}' is invalid, must be after event start '{@event.Start:u}'",
                });
            }
            if (data.Start > @event.End.AddMinutes(1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.Start),
                    AttemptedValue = data.Start,
                    ErrorMessage = $"Registration start '{data.Start:u}' is invalid, must be before event end '{@event.End:u}'",
                });
            }
            if (data.End < @event.Start.AddMinutes(-1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.End),
                    AttemptedValue = data.End,
                    ErrorMessage = $"Registration end '{data.End:u}' is invalid, must be after event start '{@event.Start:u}'",
                });
            }
            if (data.End > @event.End.AddMinutes(1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.End),
                    AttemptedValue = data.End,
                    ErrorMessage = $"Registration start '{data.End:u}' is invalid, must be before event end '{@event.End:u}'",
                });
            }
            if (!user.CanRegisterForEvents)
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(data.UserId),
                    AttemptedValue = data.UserId,
                    ErrorMessage = "User may not sign up for events",
                });
            }

            if (failures.Any())
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = failures
                });
            }

            data.UserId = user.Id;

            var result = await _context.EventRegistrations.AddAsync(data);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            // todo: send confirmation email

            await _loggingService.AddWebsiteLog(Request, $"Created event registration '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<EventRegistration>
            {
                StatusCode = 200,
                Message = $"Created event position '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("own/{eventId:int}")]
    [Authorize]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(Response<EventRegistration>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventRegistration>>> GetOwnEventRegistration(int eventId)
    {
        try
        {
            if (!await _context.Events.AnyAsync(x => x.Id == eventId))
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }
            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var result = await _context.EventRegistrations.FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == user.Id);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Event registration not found"
                });
            }

            return Ok(new Response<EventRegistration>
            {
                StatusCode = 200,
                Message = $"Got event registration '{result.Id}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetOwnEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("Registrations/{eventId:int}")]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<IList<EventRegistration>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<EventRegistration>>>> GetEventRegistrations(int eventId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

            if (!await _context.Events.AnyAsync(x => x.Id == eventId))
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }

            var result = await _context.EventRegistrations.Where(x => x.EventId == eventId).ToListAsync();
            return Ok(new Response<IList<EventRegistration>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} event registrations",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEventRegistrations error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("assign/{eventRegistrationId:int}")]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<EventRegistration>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventRegistration>>> AssignEventRegistration(int eventRegistrationId, bool relief)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

            var eventRegistration = await _context.EventRegistrations.FindAsync(eventRegistrationId);
            if (eventRegistration == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event registration '{eventRegistrationId}' not found"
                });
            }

            if (relief)
            {
                var oldDataRelief = JsonConvert.SerializeObject(eventRegistration);
                eventRegistration.Status = EventRegistrationStatus.RELIEF;
                eventRegistration.Updated = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                var newDataRelief = JsonConvert.SerializeObject(eventRegistration);

                // todo: send email

                await _loggingService.AddWebsiteLog(Request,
                    $"Assigned event registration '{eventRegistrationId}' to relief", oldDataRelief, newDataRelief);

                return Ok(new Response<EventRegistration>
                {
                    StatusCode = 200,
                    Message = $"Assign event registration '{eventRegistration.Id}' as relief",
                    Data = eventRegistration
                });
            }

            var eventPosition = await _context.EventPositions.FindAsync(eventRegistration.EventPositionId);
            if (eventPosition == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event position '{eventRegistration.EventPositionId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(eventRegistration);
            eventRegistration.Status = EventRegistrationStatus.ASSIGNED;
            eventRegistration.Updated = DateTimeOffset.UtcNow;
            eventPosition.Available = false;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(eventRegistration);

            // todo: send email

            await _loggingService.AddWebsiteLog(Request,
                $"Assigned event registration '{eventRegistrationId}' to event position '{eventPosition.Id}'", oldData, newData);

            return Ok(new Response<EventRegistration>
            {
                StatusCode = 200,
                Message = $"Assign event registration '{eventRegistration.Id}'",
                Data = eventRegistration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("AssignEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("own/{eventId:int}")]
    [Authorize]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> DeleteOwnEventRegistration(int eventId)
    {
        try
        {
            if (!await _context.Events.AnyAsync(x => x.Id == eventId))
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }
            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }
            var registration = await _context.EventRegistrations.FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == user.Id);
            if (registration == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Event registration not found"
                });
            }
            var position = await _context.EventPositions.FindAsync(registration.EventPositionId);
            if (position == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Event position not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(registration);
            _context.EventRegistrations.Remove(registration);
            await _context.SaveChangesAsync();

            position.Available = true;
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"User deleted event registration '{registration.Id}'", oldData, string.Empty);

            // todo: send confirmation email
            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted event registration '{registration.Id}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteOwnEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{evenRegistrationtId:int}")]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> DeleteEventRegistration(int eventRegistrationId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

            var registration = await _context.EventRegistrations.FindAsync(eventRegistrationId);
            if (registration == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event registration '{eventRegistrationId}' not found"
                });
            }
            var position = await _context.EventPositions.FindAsync(registration.EventPositionId);
            if (position == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event position '{registration.EventPositionId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(registration);
            _context.EventRegistrations.Remove(registration);
            await _context.SaveChangesAsync();

            position.Available = true;
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted event registration '{registration.Id}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = "Deleted event registration"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
