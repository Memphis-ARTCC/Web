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
public class CommentsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<Comment> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<AirportsController> _logger;

    public CommentsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<Comment> validator, IHub sentryHub, ILogger<AirportsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CAN_COMMENT)]
    [ProducesResponseType(typeof(Response<Comment>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Comment>>> CreateComment(Comment data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_COMMENT_LIST))
                return StatusCode(401);

            // Check if they can add a confidential comment
            if (data.Confidential && !await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_COMMENT_CONFIDENTIAL_LIST))
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

            if (!await _context.Users.AnyAsync(x => x.Id == data.UserId))
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"User '{data.UserId}' not found",
                    Data = data.UserId
                });
            }

            var submitter = Request.HttpContext.GetCid();
            if (submitter == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "Submitter not found",
                    Data = 0
                });
            }

            data.SubmitterId = submitter ?? 0;

            var result = await _context.Comments.AddAsync(data);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created comment '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<Comment>
            {
                StatusCode = 200,
                Message = $"Created comment '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateComment error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{userId:int}")]
    [Authorize(Roles = $"{Constants.CAN_COMMENT},{Constants.CAN_COMMENT_CONFIDENTIAL}")]
    [ProducesResponseType(typeof(ResponsePaging<IList<Comment>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Comment>>>> GetComments(int userId, int page = 1, int size = 10)
    {
        try
        {
            if (!await _context.Users.AnyAsync(x => x.Id == userId))
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"User '{userId}' not found",
                    Data = userId
                });
            }
            if (await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_COMMENT_CONFIDENTIAL_LIST))
            {
                var confidentialResult = await _context.Comments
                    .Where(x => x.UserId == userId)
                    .OrderBy(x => x.Timestamp)
                    .Skip((page - 1) * size).Take(size)
                    .ToListAsync();
                var confidentialTotalCount = await _context.Comments
                    .Where(x => x.UserId == userId).CountAsync();
                return Ok(new ResponsePaging<IList<Comment>>
                {
                    StatusCode = 200,
                    ResultCount = confidentialResult.Count,
                    TotalCount = confidentialTotalCount,
                    Message = $"Got {confidentialResult.Count} comments",
                    Data = confidentialResult
                });
            }
            else if (await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CAN_COMMENT_LIST))
            {
                var result = await _context.Comments
                    .Where(x => x.UserId == userId)
                    .Where(x => !x.Confidential)
                    .OrderBy(x => x.Timestamp)
                    .Skip((page - 1) * size).Take(size)
                    .ToListAsync();
                var totalCount = await _context.Comments
                    .Where(x => x.UserId == userId)
                    .Where(x => !x.Confidential)
                    .OrderBy(x => x.Timestamp).CountAsync();
                return Ok(new ResponsePaging<IList<Comment>>
                {
                    StatusCode = 200,
                    ResultCount = result.Count,
                    TotalCount = totalCount,
                    Message = $"Got {result.Count} comments",
                    Data = result
                });
            }
            return StatusCode(401);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetComments error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{commentId:int}")]
    [Authorize(Roles = Constants.SENIOR_STAFF)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteComment(int commentId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SENIOR_STAFF_LIST))
                return StatusCode(401);

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"Comment '{commentId}' not found",
                    Data = commentId
                });
            }
            var oldData = JsonConvert.SerializeObject(comment);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted comment '{commentId}'", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted comment '{commentId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetComments error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
