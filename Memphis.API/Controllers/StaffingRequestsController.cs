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

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class StaffingRequestsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<StaffingRequestPayload> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<NewsController> _logger;

    public StaffingRequestsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<StaffingRequestPayload> validator, ISentryClient sentryHub, ILogger<NewsController> logger)
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
    [ProducesResponseType(typeof(Response<News>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> CreateStaffingRequest(StaffingRequestPayload payload)
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

            var result = await _context.StaffingRequests.AddAsync(new StaffingRequest
            {
                Cid = payload.Cid,
                FullName = payload.FullName,
                Email = payload.Email,
                Organization = payload.Organization,
                EstimatedPilots = payload.EstimatedPilots,
                Start = payload.Start,
                Duration = payload.Duration,
            });
            await _context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Created staffing request {result.Entity.Id}", string.Empty, newData);

            return StatusCode(201, new Response<string>
            {
                StatusCode = 201,
                Message = $"Created staffing request '{result.Entity.Id}'",
                Data = string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateStaffingRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [Authorize(Roles = Constants.FacilitiesStaff)]
    [ProducesResponseType(typeof(Response<StatsDto>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<StaffingRequest>>>> GetStaffingRequests(StaffingRequestStatus status)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.FacilitiesStaffList))
            {
                return StatusCode(401);
            }

            var result = await _context.StaffingRequests
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.Timetstamp)
                .ToListAsync();

            return Ok(new Response<IList<StaffingRequest>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} staffing requests",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStaffingRequests error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
