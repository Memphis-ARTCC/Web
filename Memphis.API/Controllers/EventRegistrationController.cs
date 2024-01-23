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
public class EventRegistrationController(DatabaseContext context, RedisService redisService,
        LoggingService loggingService, IValidator<EventRegistrationDto> validator,
        ISentryClient sentryHub, ILogger<EventRegistrationController> logger)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Constants.CanRegisterForEvents)]
    [ProducesResponseType(typeof(Response<EventRegistration>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventRegistration>>> CreateEventRegistration(EventRegistrationDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User,
                    new[] { Constants.CanRegisterForEvents }))
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

            var @event = await context.Events.FindAsync(payload.EventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{payload.EventId}' not found"
                });
            }

            var position = await context.EventPositions.FindAsync(payload.EventPositionId);
            if (position == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event position '{payload.EventPositionId}' not found"
                });
            }

            var user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var existingRegistrations = await context.EventRegistrations
                .AnyAsync(x => x.Event == @event && x.User == user);
            var failures = new List<ValidationFailure>();
            if (existingRegistrations)
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.EventId),
                    AttemptedValue = payload.EventId,
                    ErrorMessage = $"User already has an event registration for event '{payload.EventId}'",
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

            if (payload.Start < @event.Start.AddMinutes(-1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.Start),
                    AttemptedValue = payload.Start,
                    ErrorMessage =
                        $"Registration start '{payload.Start:u}' is invalid, must be after event start '{@event.Start:u}'",
                });
            }

            if (payload.Start > @event.End.AddMinutes(1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.Start),
                    AttemptedValue = payload.Start,
                    ErrorMessage =
                        $"Registration start '{payload.Start:u}' is invalid, must be before event end '{@event.End:u}'",
                });
            }

            if (payload.End < @event.Start.AddMinutes(-1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.End),
                    AttemptedValue = payload.End,
                    ErrorMessage =
                        $"Registration end '{payload.End:u}' is invalid, must be after event start '{@event.Start:u}'",
                });
            }

            if (payload.End > @event.End.AddMinutes(1))
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.End),
                    AttemptedValue = payload.End,
                    ErrorMessage =
                        $"Registration start '{payload.End:u}' is invalid, must be before event end '{@event.End:u}'",
                });
            }

            if (!user.CanRegisterForEvents)
            {
                failures.Add(new ValidationFailure
                {
                    PropertyName = nameof(payload.EventId),
                    AttemptedValue = payload.EventId,
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

            var result = await context.EventRegistrations.AddAsync(new EventRegistration
            {
                User = user,
                Event = @event,
                EventPosition = position,
                Start = payload.Start,
                End = payload.End
            });
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            // todo: send confirmation email

            await loggingService.AddWebsiteLog(Request, $"Created event registration '{result.Entity.Id}'",
                string.Empty, newData);

            return StatusCode(201, new Response<EventRegistration>
            {
                StatusCode = 201,
                Message = $"Created event position '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var @event = await context.Events.FindAsync(eventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }

            var user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var result =
                await context.EventRegistrations.FirstOrDefaultAsync(x => x.Event == @event && x.User == user);
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
            logger.LogError("GetOwnEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("Registrations/{eventId:int}")]
    [Authorize(Roles = Constants.CanEvents)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<IList<EventRegistration>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<EventRegistration>>>> GetEventRegistrations(int eventId)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanEventsList))
                return StatusCode(401);

            var @event = await context.Events.FindAsync(eventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }

            var result = await context.EventRegistrations.Where(x => x.Event == @event).ToListAsync();
            return Ok(new Response<IList<EventRegistration>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} event registrations",
                Data = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetEventRegistrations error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("assign/{eventRegistrationId:int}")]
    [Authorize(Roles = Constants.CanEvents)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<EventRegistration>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<EventRegistration>>> AssignEventRegistration(int eventRegistrationId,
        bool relief)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanEventsList))
                return StatusCode(401);

            var eventRegistration = await context.EventRegistrations.Include(x => x.EventPosition)
                .FirstOrDefaultAsync(x => x.Id == eventRegistrationId);
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
                await context.SaveChangesAsync();
                var newDataRelief = JsonConvert.SerializeObject(eventRegistration);

                // todo: send email

                await loggingService.AddWebsiteLog(Request,
                    $"Assigned event registration '{eventRegistrationId}' to relief", oldDataRelief, newDataRelief);

                return Ok(new Response<EventRegistration>
                {
                    StatusCode = 200,
                    Message = $"Assign event registration '{eventRegistration.Id}' as relief",
                    Data = eventRegistration
                });
            }

            var oldData = JsonConvert.SerializeObject(eventRegistration);
            eventRegistration.Status = EventRegistrationStatus.ASSIGNED;
            eventRegistration.Updated = DateTimeOffset.UtcNow;
            eventRegistration.EventPosition.Available = false;
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(eventRegistration);

            // todo: send email

            await loggingService.AddWebsiteLog(Request,
                $"Assigned event registration '{eventRegistrationId}' to event position '{eventRegistration.EventPosition.Id}'",
                oldData, newData);

            return Ok(new Response<EventRegistration>
            {
                StatusCode = 200,
                Message = $"Assign event registration '{eventRegistration.Id}'",
                Data = eventRegistration
            });
        }
        catch (Exception ex)
        {
            logger.LogError("AssignEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var @event = await context.Events.FindAsync(eventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }

            var user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var registration = await context.EventRegistrations.Include(x => x.EventPosition)
                .FirstOrDefaultAsync(x => x.Event == @event && x.User == user);
            if (registration == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Event registration not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(registration);
            context.EventRegistrations.Remove(registration);
            await context.SaveChangesAsync();

            registration.EventPosition.Available = true;
            await context.SaveChangesAsync();

            await loggingService.AddWebsiteLog(Request, $"User deleted event registration '{registration.Id}'",
                oldData, string.Empty);

            // todo: send confirmation email
            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted event registration '{registration.Id}'"
            });
        }
        catch (Exception ex)
        {
            logger.LogError("DeleteOwnEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{eventRegistrationId:int}")]
    [Authorize(Roles = Constants.CanEvents)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> DeleteEventRegistration(int eventRegistrationId)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanEventsList))
                return StatusCode(401);

            var registration = await context.EventRegistrations.Include(x => x.EventPosition)
                .FirstOrDefaultAsync(x => x.Id == eventRegistrationId);
            if (registration == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event registration '{eventRegistrationId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(registration);
            context.EventRegistrations.Remove(registration);
            await context.SaveChangesAsync();

            registration.EventPosition.Available = true;
            await context.SaveChangesAsync();

            await loggingService.AddWebsiteLog(Request, $"Deleted event registration '{registration.Id}'", oldData,
                string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = "Deleted event registration"
            });
        }
        catch (Exception ex)
        {
            logger.LogError("DeleteEventRegistration error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}