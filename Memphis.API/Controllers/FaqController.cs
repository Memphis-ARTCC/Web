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
public class FaqController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<FaqDto> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<FaqController> _logger;

    public FaqController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<FaqDto> validator, IHub sentryHub, ILogger<FaqController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CanFaq)]
    [ProducesResponseType(typeof(Response<Faq>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> CreateFaq(FaqDto data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
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

            var result = await _context.Faq.AddAsync(new Faq
            {
                Question = data.Question,
                Answer = data.Answer,
                Order = data.Order,
            });
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created faq '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<Faq>
            {
                StatusCode = 200,
                Message = $"Created faq '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateFaq error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<Faq>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> GetFaqs()
    {
        try
        {
            var result = await _context.Faq.OrderBy(x => x.Order).ThenBy(x => x.Question).ToListAsync();
            return Ok(new Response<IList<Faq>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} faqs",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetFaqs error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("{faqId:int}")]
    [Authorize(Roles = Constants.CanFaq)]
    [ProducesResponseType(typeof(Response<Faq>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Faq>>> UpdateFaq(int faqId, FaqDto data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
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

            var faq = await _context.Faq.FindAsync(faqId);
            if (faq == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"FAQ '{faqId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(faq);
            faq.Question = data.Question;
            faq.Answer = data.Answer;
            faq.Order = data.Order;
            faq.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(faq);

            await _loggingService.AddWebsiteLog(Request, $"Updated faq '{faq.Id}'", oldData, newData);

            return StatusCode(200, new Response<Faq>
            {
                StatusCode = 200,
                Message = $"Updated faq '{faq.Id}'",
                Data = faq
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateFaq error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
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
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFaqList))
                return StatusCode(401);

            var faq = await _context.Faq.FindAsync(faqId);
            if (faq == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"FAQ '{faqId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(faq);
            _context.Faq.Remove(faq);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted faq '{faqId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted faq '{faqId}'"
            });
        }
        catch (Exception ex)
        {
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}