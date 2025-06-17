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
public class ExamRequestsController : ControllerBase
{

    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<ExamRequestPayload> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<ExamRequestsController> _logger;

    public ExamRequestsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<ExamRequestPayload> validator, ISentryClient sentryHub, ILogger<ExamRequestsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(Response<ExamRequestDto>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<ExamRequestDto>>> CreateExamRequest(ExamRequestPayload payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
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

            var instructor = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == Request.HttpContext.GetCid());
            if (instructor == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Instructor not found",
                    Data = null
                });
            }

            var student = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == payload.StudentId);
            if (student == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Student not found",
                    Data = null
                });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == payload.ExamId);
            if (exam == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Exam not found",
                    Data = null
                });
            }

            var result = await _context.ExamRequests.AddAsync(new ExamRequest
            {
                Instructor = instructor,
                Student = student,
                Exam = exam,
                Reason = payload.Reason
            });
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Created ExamRequest {result.Entity.Id}", string.Empty, newData);

            return StatusCode(201, new Response<ExamRequestDto>
            {
                StatusCode = 201,
                Message = $"Created exam request '{result.Entity.Id}'",
                Data = ExamRequestDto.Parse(result.Entity)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateExamRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(Response<IList<ExamRequestDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<ExamRequestDto>>>> GetExamRequests(ExamRequestStatus status)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var result = await _context.ExamRequests
                .Include(x => x.Instructor)
                .Include(x => x.Student)
                .Include(x => x.Exam)
                .Where(x => x.Status == status)
                .ToListAsync();

            return Ok(new Response<IList<ExamRequestDto>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} exam requests",
                Data = ExamRequestDto.ParseMany(result)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetExamRequests error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("{examRequestId:int}")]
    [Authorize(Roles = Constants.SeniorTrainingStaff)]
    [ProducesResponseType(typeof(Response<IList<ExamRequestDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<ExamRequestDto>>> ProcessExamRequest(int examRequestId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var examRequest = await _context.ExamRequests
                .FirstOrDefaultAsync(x => x.Id == examRequestId);
            if (examRequest == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"Exam request '{examRequestId}' not found",
                    Data = examRequestId
                });
            }

            if (examRequest.Status != ExamRequestStatus.PENDING)
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 400,
                    Message = "Exam request is not pending",
                    Data = null
                });
            }

            var oldData = JsonConvert.SerializeObject(examRequest);
            examRequest.Status = ExamRequestStatus.ASSIGNED;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(examRequest);
            await _loggingService.AddWebsiteLog(Request, $"Processed ExamRequest {examRequest.Id}", oldData, newData);

            return Ok(new Response<ExamRequestDto>
            {
                StatusCode = 200,
                Message = $"Processed exam request '{examRequest.Id}'",
                Data = ExamRequestDto.Parse(examRequest)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("ProcessExamRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    public async Task<ActionResult<Response<string?>>> DeleteExamRequest(int examRequestId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorTrainingStaffList))
            {
                return StatusCode(401);
            }

            var examRequest = await _context.ExamRequests
                .FirstOrDefaultAsync(x => x.Id == examRequestId);
            if (examRequest == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"Exam request '{examRequestId}' not found",
                    Data = examRequestId
                });
            }

            var oldData = JsonConvert.SerializeObject(examRequest);
            _context.ExamRequests.Remove(examRequest);
            await _context.SaveChangesAsync();
            await _loggingService.AddWebsiteLog(Request, $"Deleted ExamRequest {examRequest.Id}", oldData, string.Empty);

            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted exam request '{examRequestId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteExamRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
