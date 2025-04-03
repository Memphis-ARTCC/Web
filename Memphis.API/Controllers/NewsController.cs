using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
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
public class NewsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<NewsPayload> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<NewsController> _logger;

    public NewsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<NewsPayload> validator, ISentryClient sentryHub, ILogger<NewsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.FullStaff)]
    [ProducesResponseType(typeof(Response<News>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<News>>> CreateNews(NewsPayload payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.FullStaffList))
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
                    Message = "User not found",
                });
            }

            var result = await _context.News.AddAsync(new News
            {
                Title = payload.Title,
                Content = payload.Content,
                Author = $"{user.FirstName} {user.LastName}",
            });
            await _context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Created news {result.Entity.Id}", string.Empty, newData);

            return StatusCode(201, new Response<News>
            {
                StatusCode = 201,
                Message = $"Created news '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateNews error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<News>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<News>>>> GetNews(int page = 1, int size = 10)
    {
        try
        {
            var result = await _context.News.OrderBy(x => x.Created)
                .Skip((page - 1) * size).ToListAsync();
            return Ok(new Response<IList<News>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} news",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetNews error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{newsId:int}")]
    [ProducesResponseType(typeof(Response<News>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<News>>>> GetNewsEntry(int newsId)
    {
        try
        {
            var result = await _context.News.FindAsync(newsId);
            if (result == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"News '{newsId}' not found",
                    Data = newsId
                });
            }

            return Ok(new Response<News>
            {
                StatusCode = 200,
                Message = $"Got news '{result.Id}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetNewsEntry error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("{newsId:int}")]
    [Authorize(Roles = Constants.FullStaff)]
    [ProducesResponseType(typeof(Response<News>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<News>>> UpdateNews(int newsId, NewsPayload payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.FullStaffList))
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

            var news = await _context.News.FindAsync(newsId);
            if (news == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"News '{newsId}' not found",
                });
            }

            var oldData = JsonConvert.SerializeObject(news);
            news.Title = payload.Title;
            news.Content = payload.Content;
            news.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(news);

            await _loggingService.AddWebsiteLog(Request, $"Updated news '{news.Id}'", oldData, newData);

            return Ok(new Response<News>
            {
                StatusCode = 200,
                Message = $"Updated news '{news.Id}'",
                Data = news
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateNews error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{newsId:int}")]
    [Authorize(Roles = Constants.FullStaff)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> DeleteNews(int newsId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.FullStaffList))
            {
                return StatusCode(401);
            }

            var news = await _context.News.FindAsync(newsId);
            if (news == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"News '{newsId}' not found",
                    Data = newsId
                });
            }

            var oldData = JsonConvert.SerializeObject(news);
            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted news '{newsId}'", oldData, string.Empty);

            await _redisService.RemoveCached("newsId");
            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted news '{newsId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteNews error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
