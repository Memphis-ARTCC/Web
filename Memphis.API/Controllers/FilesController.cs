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
public class FilesController(DatabaseContext context, RedisService redisService, S3Service s3Service,
        LoggingService loggingService, IValidator<FileDto> validator, ISentryClient sentryHub,
        ILogger<FilesController> logger)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Constants.CanFiles)]
    [ProducesResponseType(typeof(Response<File>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<File>>> CreateFile(FileDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFilesList))
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

            var fileUrl = await s3Service.UploadFile(Request, "files");
            var result = await context.Files.AddAsync(new File
            {
                Title = payload.Title,
                Description = payload.Description,
                Version = payload.Version,
                FileUrl = fileUrl,
                Type = payload.Type
            });
            var newData = JsonConvert.SerializeObject(result.Entity);
            await loggingService.AddWebsiteLog(Request, $"Created file '{result.Entity.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<File>
            {
                StatusCode = 201,
                Message = $"Created file '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var isStaff = await redisService.ValidateRoles(Request.HttpContext.User, Constants.AllStaffList);
            var isSeniorStaff =
                await redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList);
            var isTrainingStaff =
                await redisService.ValidateRoles(Request.HttpContext.User, Constants.TrainingStaffList);
            var resultQuery = context.Files.AsQueryable();
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
            logger.LogError("GetFiles error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
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
            var result = await context.Files.FindAsync(fileId);
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
            logger.LogError("GetFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CanFiles)]
    [ProducesResponseType(typeof(Response<File>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<File>>> UpdateFile(FileDto payload)
    {
        try
        {
            if (!await redisService.ValidateRoles(Request.HttpContext.User, Constants.CanFilesList))
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

            var file = await context.Files.FindAsync(payload.Id);
            if (file == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"File '{payload.Id}' not found"
                });
            }

            var oldData = JsonConvert.SerializeObject(file);

            if (Request.Form.Files.Any())
            {
                if (file.FileUrl != null)
                {
                    await s3Service.DeleteFile(file.FileUrl);
                    var newUrl = await s3Service.UploadFile(Request, "files");
                    if (newUrl != null)
                    {
                        file.FileUrl = newUrl;
                    }
                }
            }

            file.Title = payload.Title;
            file.Description = payload.Description;
            file.Version = payload.Version;
            file.Type = payload.Type;
            await context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(file);

            await loggingService.AddWebsiteLog(Request, $"Updated file '{file.Id}'", oldData, newData);

            return Ok(new Response<File>
            {
                StatusCode = 200,
                Message = $"Updated file '{file.Id}'",
                Data = file
            });
        }
        catch (Exception ex)
        {
            logger.LogError("UpdateFile error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}