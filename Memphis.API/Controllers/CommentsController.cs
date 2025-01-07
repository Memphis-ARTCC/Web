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
    private readonly IValidator<CommentPayload> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<CommentPayload> validator, ISentryClient sentryHub, ILogger<CommentsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CanComment)]
    [ProducesResponseType(typeof(Response<Comment>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Comment>>> CreateComment(CommentPayload payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanCommentList))
            {
                return StatusCode(401);
            }

            // Check if they can add a confidential comment
            if (payload.Confidential && !await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanCommentConfidentialList))
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

            var user = await _context.Users.FindAsync(payload.UserId);
            if (user == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"User '{payload.UserId}' not found",
                    Data = payload.UserId
                });
            }

            var submitter = await Request.HttpContext.GetUser(_context);
            if (submitter == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "Submitter not found",
                    Data = 0
                });
            }

            var result = await _context.Comments.AddAsync(new Comment
            {
                User = user,
                Submitter = submitter,
                Confidential = payload.Confidential,
                Message = payload.Message,
            });
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);

            await _loggingService.AddWebsiteLog(Request, $"Created comment '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<Comment>
            {
                StatusCode = 201,
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
    [Authorize(Roles = $"{Constants.CanComment},{Constants.CanCommentConfidential}")]
    [ProducesResponseType(typeof(ResponsePaging<IList<Comment>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Comment>>>> GetComments(int userId, int page = 1, int size = 10)
    {
        try
        {
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

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"User '{userId}' not found",
                    Data = userId
                });
            }

            if (await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanCommentConfidentialList))
            {
                var confidentialResult = await _context.Comments
                    .Where(x => x.User == user)
                    .OrderBy(x => x.Timestamp)
                    .Skip((page - 1) * size).Take(size)
                    .ToListAsync();
                var confidentialTotalCount = await _context.Comments
                    .Where(x => x.User == user).CountAsync();
                return Ok(new ResponsePaging<IList<Comment>>
                {
                    StatusCode = 200,
                    ResultCount = confidentialResult.Count,
                    TotalCount = confidentialTotalCount,
                    Message = $"Got {confidentialResult.Count} comments",
                    Data = confidentialResult
                });
            }

            if (await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanCommentList))
            {
                var result = await _context.Comments
                    .Where(x => x.User == user)
                    .Where(x => !x.Confidential)
                    .OrderBy(x => x.Timestamp)
                    .Skip((page - 1) * size).Take(size)
                    .ToListAsync();
                var totalCount = await _context.Comments
                    .Where(x => x.User == user)
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
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> DeleteComment(int commentId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
            {
                return StatusCode(401);
            }

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