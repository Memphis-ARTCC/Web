using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
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
public class UsersController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<UserPayload> _userValidator;
    private readonly IValidator<CommentPayload> _commentValidator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<UserPayload> validator, IValidator<CommentPayload> commentValidator, ISentryClient sentryHub, ILogger<UsersController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _userValidator = validator;
        _commentValidator = commentValidator;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(typeof(Response<Comment>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Comment>>> AddComment(int userId, CommentPayload payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
            {
                return StatusCode(401);
            }

            var validation = await _commentValidator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Data = userId
                });
            }

            var submitter = await HttpContext.GetUser(_context);
            if (submitter == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var result = await _context.Comments.AddAsync(new Comment
            {
                User = user,
                Submitter = submitter,
                Message = payload.Message,
            });
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Added comment to user '{user.Id}'", string.Empty, newData);

            return StatusCode(201, new Response<Comment>
            {
                StatusCode = 201,
                Message = $"Added comment to user '{user.Id}'",
                Data = result.Entity
            });

        }
        catch (Exception ex)
        {
            _logger.LogError("AddComment error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("Roster")]
    [Authorize]
    [ProducesResponseType(typeof(Response<IList<RosterUserDto>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<RosterUserDto>>>> GetRoster()
    {
        try
        {
            var users = await _context.Users
                .Include(x => x.Roles)
                .Include(x => x.Ground)
                .Include(x => x.Tower)
                .Include(x => x.Tracon)
                .Include(x => x.Center)
                .Where(x => x.Status != Shared.Enums.UserStatus.REMOVED)
                .OrderBy(x => x.LastName)
                .ToListAsync();
            return Ok(new Response<IList<RosterUserDto>>
            {
                StatusCode = 200,
                Message = $"Got {users.Count} users",
                Data = RosterUserDto.ParseMany(users)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetRoster error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("Staff")]
    [ProducesResponseType(typeof(Response<StaffResponseDto>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<StaffResponseDto>>> GetStaff()
    {
        try
        {
            var atm = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "ATM")).FirstOrDefaultAsync();
            var datm = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "DATM")).FirstOrDefaultAsync();
            var ta = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "TA")).FirstOrDefaultAsync();
            var ata = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "ATA")).FirstOrDefaultAsync();
            var wm = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "WM")).FirstOrDefaultAsync();
            var awm = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "AWM")).FirstOrDefaultAsync();
            var webTeam = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "WEB")).ToListAsync();
            var ec = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "EC")).FirstOrDefaultAsync();
            var aec = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "AEC")).FirstOrDefaultAsync();
            var eventsTeam = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "EVENTS")).ToListAsync();
            var fe = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "FE")).FirstOrDefaultAsync();
            var afe = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "AFE")).FirstOrDefaultAsync();
            var facilitiesTeam = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "FACILITIES")).ToListAsync();
            var socialMediaTeam = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "SOCIAL")).ToListAsync();
            var ins = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "INS")).ToListAsync();
            var mtr = await _context.Users.Include(x => x.Roles).Where(x => x.Roles!.Any(r => r.NameShort == "MTR")).ToListAsync();

            return Ok(new Response<StaffResponseDto>
            {
                StatusCode = 200,
                Message = "Got staff",
                Data = new StaffResponseDto
                {
                    Atm = atm != null ? $"{atm.FirstName} {atm.LastName}" : null,
                    AtmEmail = "atm@memphisartcc.com",
                    Datm = datm != null ? $"{datm.FirstName} {datm.LastName}" : null,
                    DatmEmail = "datm@memphisartcc.com",
                    Ta = ta != null ? $"{ta.FirstName} {ta.LastName}" : null,
                    TaEmail = "ta@memphisartcc.com",
                    Ata = ata != null ? $"{ata.FirstName} {ata.LastName}" : null,
                    AtaEmail = "ata@memphisartcc.com",
                    Wm = wm != null ? $"{wm.FirstName} {wm.LastName}" : null,
                    WmEmail = "wm@memphisartcc.com",
                    Awm = awm != null ? $"{awm.FirstName} {awm.LastName}" : null,
                    AwmEmail = "awm@memphisartcc.com",
                    WebTeam = webTeam.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    WebTeamEmail = "web@memphisartcc.com",
                    Ec = ec != null ? $"{ec.FirstName} {ec.LastName}" : null,
                    EcEmail = "ec@memphisartcc.com",
                    Aec = aec != null ? $"{aec.FirstName} {aec.LastName}" : null,
                    AecEmail = "aec@memphisartcc.com",
                    EventsTeam = eventsTeam.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    EventsTeamEmail = "events@memphisartcc.com",
                    Fe = fe != null ? $"{fe.FirstName} {fe.LastName}" : null,
                    FeEmail = "fe@memphisartcc.com",
                    Afe = afe != null ? $"{afe.FirstName} {afe.LastName}" : null,
                    AfeEmail = "afe@memphisartcc.com",
                    FacilitiesTeam = facilitiesTeam.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    FacilitiesTeamEmail = "facilities@memphisartcc.com",
                    SocialMediaTeam = socialMediaTeam.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    SocialMediaTeamEmail = "socialmedia@memphisartcc.com",
                    Ins = ins.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    InsEmail = "instructors@memphisartcc.com",
                    Mtr = mtr.Select(x => $"{x.FirstName} {x.LastName}").ToList(),
                    MtrEmail = "mentors@memphisartcc.com",
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStaff error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("roles")]
    [Authorize(Roles = Constants.AllStaff)]
    [ProducesResponseType(typeof(Response<IList<Role>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Role>>>> GetRoles()
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.AllStaffList))
            {
                return StatusCode(401);
            }

            var roles = await _context.Roles.ToListAsync();

            return Ok(new Response<IList<Role>>
            {
                StatusCode = 200,
                Message = $"Got {roles.Count} roles",
                Data = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetRoles error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("roles/add/{userId:int}")]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(typeof(Response<IList<Role>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Role>>>> AddRole(int userId, int roleId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
            {
                return StatusCode(401);
            }

            var user = await _context.Users
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Data = userId
                });
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == roleId);
            if (role == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "Role not found",
                    Data = roleId
                });
            }

            user.Roles ??= [];

            if (user.Roles.Any(x => x.Id == roleId))
            {
                return Ok(new Response<IList<Role>>
                {
                    StatusCode = 200,
                    Message = "Role already assigned to user",
                    Data = user.Roles?.ToList()
                });
            }

            var oldData = JsonConvert.SerializeObject(user.Roles);
            user.Roles.Add(role);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(user.Roles);
            await _loggingService.AddWebsiteLog(Request, $"Added Role '{role.Name}' to user '{user.Id}'", oldData, newData);

            return Ok(new Response<IList<Role>>
            {
                StatusCode = 200,
                Message = $"Added role '{role.Name}' to user '{user.Id}'",
                Data = user.Roles.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("AddRole error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("roles/remove/{userId:int}")]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(typeof(Response<IList<Role>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Role>>>> RemoveRole(int userId, int roleId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
            {
                return StatusCode(401);
            }

            var user = await _context.Users
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Data = userId
                });
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == roleId);
            if (role == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = "Role not found",
                    Data = roleId
                });
            }

            if (user.Roles == null || !user.Roles.Any(x => x.Id == roleId))
            {
                return Ok(new Response<IList<Role>>
                {
                    StatusCode = 200,
                    Message = "Role not assigned to user",
                    Data = user.Roles?.ToList()
                });
            }

            var oldData = JsonConvert.SerializeObject(user.Roles);
            user.Roles.Remove(role);
            await _context.SaveChangesAsync();
            var newData = JsonConvert.SerializeObject(user.Roles);
            await _loggingService.AddWebsiteLog(Request, $"Removed Role '{role.Name}' from user '{user.Id}'", oldData, newData);

            return Ok(new Response<IList<Role>>
            {
                StatusCode = 200,
                Message = $"Removed role '{role.Name}' from user '{user.Id}'",
                Data = user.Roles.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("AddRole error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
