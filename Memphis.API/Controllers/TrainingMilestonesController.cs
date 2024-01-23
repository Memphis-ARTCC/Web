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
public class TrainingMilestonesController(DatabaseContext context, RedisService redisService,
        LoggingService loggingService, IValidator<TrainingMilestone> validator, ISentryClient sentryHub,
        ILogger<TrainingMilestonesController> logger)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Constants.CanTrainingMilestones)]
    [ProducesResponseType(typeof(Response<TrainingMilestone>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingMilestone>>> CreateTrainingMilestone(TrainingMilestone payload)
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

            var result = await context.TrainingMilestones.AddAsync(new TrainingMilestone
            {
                Code = payload.Code,
                Name = payload.Name,
                Facility = payload.Facility,
                Created = DateTimeOffset.UtcNow,
                Updated = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await loggingService.AddWebsiteLog(Request, $"Created training milestone '{result.Entity.Id}'",
                string.Empty, newData);

            return StatusCode(201, new Response<TrainingMilestone>
            {
                StatusCode = 201,
                Message = $"Created training milestone '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateTrainingMilestone error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(Response<TrainingMilestone>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<TrainingMilestone>>>> GetTrainingMilestones()
    {
        try
        {
            var milestones = await context.TrainingMilestones.ToListAsync();
            return Ok(new Response<IList<TrainingMilestone>>
            {
                StatusCode = 200,
                Message = $"Got {milestones.Count} training milestones",
                Data = milestones
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetTrainingMilestones error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{milestoneId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(Response<TrainingMilestone>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingMilestone>>> GetTrainingMilestone(int milestoneId)
    {
        try
        {
            var milestone = await context.TrainingMilestones.FindAsync(milestoneId);
            if (milestone == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Training milestone '{milestoneId}' not found"
                });
            }

            return Ok(new Response<TrainingMilestone>
            {
                StatusCode = 200,
                Message = $"Got training milestone '{milestoneId}'",
                Data = milestone
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetTrainingMilestones error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CanTrainingMilestones)]
    [ProducesResponseType(typeof(Response<TrainingMilestone>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingMilestone>>> UpdateTrainingMilestone(TrainingMilestone payload)
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

            var milestone = await context.TrainingMilestones.FindAsync(payload.Id);
            if (milestone == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Training milestone '{payload.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(milestone);
            milestone.Code = payload.Code;
            milestone.Name = payload.Name;
            milestone.Facility = payload.Facility;
            milestone.Updated = payload.Updated;
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(milestone);

            await loggingService.AddWebsiteLog(Request, $"Updated training milestone '{milestone.Id}'", oldData,
                newData);

            return Ok(new Response<TrainingMilestone>
            {
                StatusCode = 200,
                Message = $"Updated training milestone '{milestone.Id}'",
                Data = milestone
            });
        }
        catch (Exception ex)
        {
            logger.LogError("UpdateTrainingMilestone error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}