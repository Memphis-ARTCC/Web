using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Constants = Memphis.Shared.Utils.Constants;


namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TrainingSchedulesController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<TrainingScheduleDto> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<TrainingSchedulesController> _logger;

    public TrainingSchedulesController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<TrainingScheduleDto> validator, ISentryClient sentryHub, ILogger<TrainingSchedulesController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CanRequestTraining)]
    [ProducesResponseType(typeof(Response<TrainingSchedule>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingSchedule>>> CreateTrainingSchedule(TrainingScheduleDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanRequestTrainingList))
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

            var user = await Request.HttpContext.GetUser(_context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var trainingTypes = new List<TrainingType>();
            foreach (int entry in payload.TrainingTypes)
            {
                var trainingType = await _context.TrainingTypes.FindAsync(entry);
                if (trainingType == null)
                {
                    return NotFound(new Response<string?>
                    {
                        StatusCode = 404,
                        Message = $"Training type '{entry}' not found"
                    });
                }

                trainingTypes.Add(trainingType);
            }

            var result = await _context.TrainingSchedules.AddAsync(new TrainingSchedule
            {
                Instructor = user,
                TrainingTypes = trainingTypes,
                Start = payload.Start
            });
            await _context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created training schedule '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<TrainingSchedule>
            {
                StatusCode = 201,
                Message = $"Created training schedule '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateTrainingSchedule error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}