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
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sentry;
using Constants = Memphis.Shared.Utils.Constants;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FaqController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<FaqDto> validator, ISentryClient sentryHub, ILogger<FaqController> logger)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Constants.CanFaq)]
    [ProducesResponseType(typeof(Response<Faq>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> CreateFaq(FaqDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
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

            var result = await context.Faq.AddAsync(new Faq
            {
                Question = payload.Question,
                Answer = payload.Answer,
                Order = payload.Order,
            });
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await loggingService.AddWebsiteLog(Request, $"Created faq '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<Faq>
            {
                StatusCode = 201,
                Message = $"Created faq '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateFaq error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<Faq>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> GetFaqs()
    {
        try
        {
            var result = await context.Faq.OrderBy(x => x.Order).ThenBy(x => x.Question).ToListAsync();
            return Ok(new Response<IList<Faq>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} faqs",
                Data = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetFaqs error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CanFaq)]
    [ProducesResponseType(typeof(Response<Faq>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> UpdateFaq(FaqDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
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

            var faq = await context.Faq.FindAsync(payload.Id);
            if (faq == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"FAQ '{payload.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(faq);
            faq.Question = payload.Question;
            faq.Answer = payload.Answer;
            faq.Order = payload.Order;
            faq.Updated = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(faq);

            await loggingService.AddWebsiteLog(Request, $"Updated faq '{faq.Id}'", oldData, newData);

            return StatusCode(200, new Response<Faq>
            {
                StatusCode = 200,
                Message = $"Updated faq '{faq.Id}'",
                Data = faq
            });
        }
        catch (Exception ex)
        {
            logger.LogError("UpdateFaq error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{faqId:int}")]
    [Authorize(Roles = Constants.CanFaq)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteFaq(int faqId)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
                return StatusCode(401);

            var faq = await context.Faq.FindAsync(faqId);
            if (faq == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"FAQ '{faqId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(faq);
            context.Faq.Remove(faq);
            await context.SaveChangesAsync();

            await loggingService.AddWebsiteLog(Request, $"Deleted faq '{faqId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted faq '{faqId}'"
            });
        }
        catch (Exception ex)
        {
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}