using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sentry;
using Constants = Memphis.Shared.Utils.Constants;


namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TrainingSchedulesController(
    DatabaseContext context,
    RedisService redisService,
    LoggingService loggingService,
    IValidator<TrainingScheduleDto> validator,
    ISentryClient sentryHub,
    ILogger<TrainingSchedulesController> logger)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Constants.CanTrainingSchedule)]
    [ProducesResponseType(typeof(Response<TrainingSchedule>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingSchedule>>> CreateTrainingSchedule(TrainingScheduleDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanTrainingMilestonesList))
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

            Shared.Models.User? user = await Request.HttpContext.GetUser(context);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            List<TrainingType> trainingTypes = [];
            foreach (int entry in payload.TrainingTypes)
            {
                TrainingType? trainingType = await context.TrainingTypes.FindAsync(entry);
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

            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TrainingSchedule> result = await context.TrainingSchedules.AddAsync(new TrainingSchedule
            {
                Instructor = user,
                TrainingTypes = trainingTypes,
                Start = payload.Start
            });
            await context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(result.Entity);

            await loggingService.AddWebsiteLog(Request, $"Created training schedule '{result.Entity.Id}'", string.Empty,
                newData);

            return StatusCode(201, new Response<TrainingSchedule>
            {
                StatusCode = 201,
                Message = $"Created training schedule '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateTrainingSchedule error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}