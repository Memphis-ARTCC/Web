using FluentValidation.Results;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
using Memphis.Shared.Enums;
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
public class OtsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<OtsController> _logger;

    public OtsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        ISentryClient sentryHub, ILogger<OtsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(ResponsePaging<IList<OtsDto>>), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<OtsDto>>>> GetOtsList(int page = 1, int size = 10)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            if (page < 1)
            {
                return BadRequest(new Response<string>
                {
                    StatusCode = 400,
                    Message = "Invalid page"
                });
            }

            if (size < 1)
            {
                return BadRequest(new Response<string>
                {
                    StatusCode = 400,
                    Message = "Invalid size"
                });
            }

            var result = await _context.Ots
                .Include(x => x.Submitter).Include(x => x.User)
                .Include(x => x.Instructor).Include(x => x.Milestone)
                .OrderBy(x => x.Status).ThenBy(x => x.Updated)
                .Skip((page - 1) * size).Take(size).ToListAsync();
            var resultCount = await _context.Ots.CountAsync();
            return StatusCode(201, new ResponsePaging<IList<OtsDto>>
            {
                StatusCode = 201,
                ResultCount = result.Count,
                TotalCount = resultCount,
                Message = $"Got {result.Count} ots's",
                Data = OtsDto.ParseMany(result)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetOtsList error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{otsId:int}")]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(ResponsePaging<IList<OtsDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<OtsDto>>> GetOts(int otsId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var result = await _context.Ots
                .Include(x => x.Submitter)
                .Include(x => x.User)
                .Include(x => x.Instructor)
                .FirstOrDefaultAsync(x => x.Id == otsId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"OTS '{otsId}' not found"
                });
            }

            return Ok(new Response<OtsDto>
            {
                StatusCode = 200,
                Message = $"Got OTS '{otsId}'",
                Data = OtsDto.Parse(result)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetOts error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(Response<OtsDto>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<OtsDto>>> UpdateOts(UpdateOtsDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var ots = await _context.Ots.FindAsync(payload.Id);
            if (ots == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"OTS '{payload.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(ots);
            if (payload.Instructor != null)
            {
                var instructor = await _context.Users.FindAsync(payload.Instructor);
                if (instructor == null)
                {
                    return NotFound(new Response<string?>
                    {
                        StatusCode = 404,
                        Message = $"User '{payload.Instructor}' not found"
                    });
                }

                ots.Instructor = instructor;
            }
            else
            {
                ots.Instructor = null;
            }

            ots.Start = payload.Start;
            ots.Milestone = payload.Milestone;
            ots.Facility = payload.Facility;
            ots.Result = payload.Result;

            if (payload.Instructor != null && payload.Start != null)
            {
                ots.Status = OtsStatus.SCHEDULED;
            }
            else if (payload.Start != null)
            {
                ots.Status = OtsStatus.SCHEDULED;
            }
            else if (payload.Instructor != null)
            {
                ots.Status = OtsStatus.ASSIGNED;
            }

            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(ots);

            await _loggingService.AddWebsiteLog(Request, $"Updated OTS '{payload.Id}'", oldData, newData);

            return Ok(new Response<OtsDto>
            {
                StatusCode = 200,
                Message = $"Updated OTS '{payload.Id}'",
                Data = OtsDto.Parse(ots)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateOts error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{otsId:int}")]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteOts(int otsId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var ots = await _context.Ots.FindAsync(otsId);
            if (ots == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"OTS '{otsId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(ots);
            _context.Ots.Remove(ots);
            await _context.SaveChangesAsync();
            await _loggingService.AddWebsiteLog(Request, $"Deleted OTS '{otsId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted OTS '{otsId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteOts error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}