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
public class EventsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly S3Service _s3Service;
    private readonly IValidator<Event> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<EventsController> _logger;

    public EventsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        S3Service s3Service, IValidator<Event> validator, IHub sentryHub, ILogger<EventsController> logger)
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
    [ProducesResponseType(typeof(Response<Comment>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Event>>> CreateEvent(Event data)
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

            var errors = new List<ValidationFailure>();
            if (data.Start <= DateTimeOffset.UtcNow)
                errors.Add(new ValidationFailure(nameof(data.Start), "Start time must be in the future"));
            if (data.Start > data.End)
                errors.Add(new ValidationFailure(nameof(data.Start), "Start time must be before end time"));
            if (data.End <= DateTimeOffset.UtcNow)
                errors.Add(new ValidationFailure(nameof(data.End), "End time must be in the future"));
            if (errors.Count > 0)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = errors
                });
            }

            data.BannerUrl = await _s3Service.UploadFile(Request, "events");
            var result = await _context.Events.AddAsync(data);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created event '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<Event>
            {
                StatusCode = 200,
                Message = $"Created event '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateEvent error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponsePaging<IList<Event>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Event>>> GetEvents(int page = 1, int size = 10)
    {
        try
        {
            var getClosed = await _redisService.ValidateRoles(Request.HttpContext.User, Constants.ALL_STAFF_LIST);
            if (getClosed)
            {
                var result = await _context.Events.OrderBy(x => x.Start).Skip((page - 1) * size).Take(size).ToListAsync();
                var totalCount = await _context.Events.Where(x => x.IsOpen).OrderBy(x => x.Start).CountAsync();
                return Ok(new ResponsePaging<IList<Event>>
                {
                    StatusCode = 200,
                    ResultCount = result.Count,
                    TotalCount = totalCount,
                    Message = $"Got {result.Count} events",
                    Data = result
                });
            }
            else
            {
                var result = await _context.Events.OrderBy(x => x.Start).Where(x => x.IsOpen).Skip((page - 1) * size).Take(size).ToListAsync();
                var totalCount = await _context.Events.Where(x => x.IsOpen).OrderBy(x => x.Start).CountAsync();
                return Ok(new ResponsePaging<IList<Event>>
                {
                    StatusCode = 200,
                    ResultCount = result.Count,
                    TotalCount = totalCount,
                    Message = $"Got {result.Count} events",
                    Data = result
                });
            }

        }
        catch (Exception ex)
        {
            _logger.LogError("GetEvents error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{eventId:int}")]
    [ProducesResponseType(typeof(Response<Event>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Event>>> GetEvent(int eventId)
    {
        try
        {
            var getClosed = await _redisService.ValidateRoles(Request.HttpContext.User, Constants.ALL_STAFF_LIST);
            var result = await _context.Events.FindAsync(eventId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }
            if (!getClosed && !result.IsOpen)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }
            return Ok(new Response<Event>
            {
                StatusCode = 200,
                Message = $"Got event '{eventId}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEvent error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(typeof(Response<Event>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Event>>> UpdateEvent(Event data)
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

            var @event = await _context.Events.FindAsync(data.Id);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{data.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(@event);

            var newFile = Request.Form.Files.Any();
            if (newFile && @event.BannerUrl != null)
            {
                await _s3Service.DeleteFile(@event.BannerUrl);
            }

            @event.Title = data.Title;
            @event.Description = data.Description;
            @event.Host = data.Host;
            if (newFile)
            {
                @event.BannerUrl = await _s3Service.UploadFile(Request, "events");
            }
            @event.Start = data.Start;
            @event.End = data.End;
            @event.IsOpen = data.IsOpen;
            @event.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(@event);

            await _loggingService.AddWebsiteLog(Request, $"Updated event '{@event.Id}'", oldData, newData);

            return Ok(new Response<Event>
            {
                StatusCode = 200,
                Message = $"Created event '{@event.Id}'",
                Data = @event
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateEvent error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }


    [HttpDelete("{eventId:int}")]
    [Authorize(Roles = Constants.CAN_EVENTS)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteEvent(int eventId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_EVENTS_LIST))
                return StatusCode(401);

            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Event '{eventId}' not found"
                });
            }

            if (@event.BannerUrl != null)
            {
                await _s3Service.DeleteFile(@event.BannerUrl);
            }

            var oldData = JsonConvert.SerializeObject(@event);
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted event '{eventId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted event '{eventId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEvent error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
