using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Enums;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sentry;
using Constants = Memphis.Shared.Utils.Constants;
using File = Memphis.Shared.Models.File;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly S3Service _s3Service;
    private readonly LoggingService _loggingService;
    private readonly IValidator<FileDto> _validator;
    private readonly IHub _sentryHub;
    private readonly ILogger<FilesController> _logger;

    public FilesController(DatabaseContext context, RedisService redisService, S3Service s3Service,
        LoggingService loggingService, IValidator<FileDto> validator, IHub sentryHub, ILogger<FilesController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
        _s3Service = s3Service;
    }

    [HttpPost]
    [Authorize(Roles = Constants.CanFiles)]
    [ProducesResponseType(typeof(Response<File>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<File>>> CreateFile(FileDto data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFilesList))
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

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "No file to upload",
                    Data = new List<ValidationFailure>
                    {
                        new ValidationFailure
                        {
                            PropertyName = "file",
                            AttemptedValue = file,
                            ErrorMessage = "No file to upload"
                        }
                    }
                });
            }

            var fileUrl = await _s3Service.UploadFile(Request, "files");
            var result = await _context.Files.AddAsync(new File
            {
                Title = data.Title,
                Description = data.Description,
                Version = data.Version,
                FileUrl = fileUrl,
                Type = data.Type
            });
            var newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Created file '{result.Entity.Id}'", string.Empty, newData);

            return Ok(new Response<File>
            {
                StatusCode = 200,
                Message = $"Created file '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<File>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<File>>>> GetFiles()
    {
        try
        {
            var isStaff = await _redisService.ValidateRoles(Request.HttpContext.User, Constants.AllStaffList);
            var isSeniorStaff =
                await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList);
            var isTrainingStaff =
                await _redisService.ValidateRoles(Request.HttpContext.User, Constants.TrainingStaffList);
            var resultQuery = _context.Files.AsQueryable();
            if (!isStaff)
            {
                resultQuery = resultQuery.Where(x => x.Type != FileType.STAFF);
            }

            if (!isSeniorStaff)
            {
                resultQuery = resultQuery.Where(x => x.Type != FileType.SENIOR_STAFF);
            }

            if (!isTrainingStaff)
            {
                resultQuery = resultQuery.Where(x => x.Type != FileType.TRAINING_STAFF);
            }

            var result = await resultQuery.ToListAsync();
            return Ok(new Response<IList<File>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} files",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetFiles error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{fileId:int}")]
    [ProducesResponseType(typeof(Response<File>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<File>>> GetFile(int fileId)
    {
        try
        {
            var result = await _context.Files.FindAsync(fileId);
            if (result == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 400,
                    Message = $"File '{fileId}' not found"
                });
            }

            return Ok(new Response<File>
            {
                StatusCode = 200,
                Message = $"Got file '{fileId}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("{fileId:int}")]
    [Authorize(Roles = Constants.CanFiles)]
    [ProducesResponseType(typeof(Response<File>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<File>>> UpdateFile(int fileId, FileDto data)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFilesList))
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

            var file = await _context.Files.FindAsync(fileId);
            if (file == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"File '{fileId}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(file);

            if (Request.Form.Files.Any())
            {
                if (file.FileUrl != null)
                {
                    await _s3Service.DeleteFile(file.FileUrl);
                    var newUrl = await _s3Service.UploadFile(Request, "files");
                    if (newUrl != null)
                    {
                        file.FileUrl = newUrl;
                    }
                }
            }

            file.Title = data.Title;
            file.Description = data.Description;
            file.Version = data.Version;
            file.Type = data.Type;
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(file);

            await _loggingService.AddWebsiteLog(Request, $"Updated file '{fileId}'", oldData, newData);

            return Ok(new Response<File>
            {
                StatusCode = 200,
                Message = $"Updated file '{fileId}'",
                Data = file
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}