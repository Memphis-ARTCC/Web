using Memphis.API.Extensions;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos;
using Memphis.Shared.Enums;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<StatsController> _logger;

    public StatsController(DatabaseContext context, ISentryClient sentryHub, ILogger<StatsController> logger)
    {
        _context = context;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet("atc")]
    [Authorize]
    [ProducesResponseType(typeof(Response<AtcStatsDto>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<AtcStatsDto>>> GetStats(DateTime start, DateTime end)
    {
        try
        {
            if (start >= end)
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 400,
                    Message = "Start must be before end"
                });
            }

            var startReal = new DateTimeOffset(start).ToUniversalTime();
            var endReal = new DateTimeOffset(end).ToUniversalTime();

            var s1Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.S1)
                .CountAsync();
            var s2Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.S2)
                .CountAsync();
            var s3Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.S3)
                .CountAsync();
            var c1Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.C1)
                .CountAsync();
            var c3Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.C3)
                .CountAsync();
            var i1Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.I1)
                .CountAsync();
            var i3Sessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.I3)
                .CountAsync();
            var supSessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.SUP)
                .CountAsync();
            var admSessions = await _context.Sessions
                .Include(x => x.User)
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.User.Rating == Rating.ADM)
                .CountAsync();
            var deliverySessions = await _context.Sessions
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.Callsign.ToUpper().EndsWith("_DEL"))
                .CountAsync();
            var groundSessions = await _context.Sessions
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.Callsign.ToUpper().EndsWith("_GND"))
                .CountAsync();
            var towerSessions = await _context.Sessions
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.Callsign.ToUpper().EndsWith("_TWR"))
                .CountAsync();
            var traconSessions = await _context.Sessions
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.Callsign.ToUpper().EndsWith("_APP") ||
                            x.Callsign.ToUpper().EndsWith("_DEP"))
                .CountAsync();
            var centerSessions = await _context.Sessions
                .Where(x => x.Start >= startReal && x.End <= endReal)
                .Where(x => x.Callsign.ToUpper().EndsWith("_CTR"))
                .CountAsync();
            return Ok(new Response<AtcStatsDto>
            {
                StatusCode = 200,
                Message = "Got stats",
                Data = new AtcStatsDto
                {
                    S1Sessions = s1Sessions,
                    S2Sessions = s2Sessions,
                    S3Sessions = s3Sessions,
                    C1Sessions = c1Sessions,
                    C3Sessions = c3Sessions,
                    I1Sessions = i1Sessions,
                    I3Sessions = i3Sessions,
                    SupSessions = supSessions,
                    AdmSessions = admSessions,
                    DeliverySessions = deliverySessions,
                    GroundSessions = groundSessions,
                    TowerSessions = towerSessions,
                    TraconSessions = traconSessions,
                    CenterSessions = centerSessions
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStats error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("training")]
    [Authorize]
    [ProducesResponseType(typeof(Response<IList<Dictionary<TrainingMilestone, int>>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<TrainingStatsDto>>> GetTrainingStats(DateTime start, DateTime end)
    {
        try
        {
            if (start >= end)
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 400,
                    Message = "Start must be before end"
                });
            }

            var startReal = new DateTimeOffset(start).ToUniversalTime();
            var endReal = new DateTimeOffset(end).ToUniversalTime();

            var result = new List<Dictionary<TrainingMilestone, int>>();
            var milestones = await _context.TrainingMilestones.ToListAsync();

            foreach (var entry in milestones)
            {
                var count = await _context.TrainingTickets
                    .Include(x => x.Milestone)
                    .Where(x => x.Start >= startReal && x.End <= endReal)
                    .CountAsync();
                result.Add(new Dictionary<TrainingMilestone, int>()
                {
                    { entry, count }
                });
            }

            return Ok(new Response<IList<Dictionary<TrainingMilestone, int>>>
            {
                StatusCode = 200,
                Message = "Got stats",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTrainingStats error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("trainingstaff")]
    [Authorize]
    [ProducesResponseType(typeof(Response<IList<Dictionary<RosterUserDto, int>>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Dictionary<RosterUserDto, int>>>>> GetTrainingStaffStats(
        DateTime start, DateTime end)
    {
        try
        {
            if (start >= end)
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 400,
                    Message = "Start must be before end"
                });
            }

            var startReal = new DateTimeOffset(start).ToUniversalTime();
            var endReal = new DateTimeOffset(end).ToUniversalTime();

            var users = await _context.Users
                .Include(x => x.Roles)
                .Where(x => x.Roles != null && (
                    x.Roles.Any(c => c.NameShort == "MTR") ||
                    x.Roles.Any(c => c.NameShort == "INS")))
                .ToListAsync();

            var result = new List<Dictionary<RosterUserDto, int>>();
            foreach (var entry in users)
            {
                var rosterUser = RosterUserDto.Parse(entry);
                var trainingCount = await _context.TrainingTickets
                    .Include(x => x.User)
                    .Where(x => x.Start >= startReal && x.End <= endReal)
                    .Where(x => x.Trainer == entry)
                    .CountAsync();
                result.Add(new Dictionary<RosterUserDto, int>
                {
                    { rosterUser, trainingCount }
                });
            }

            return Ok(new Response<IList<Dictionary<RosterUserDto, int>>>
            {
                StatusCode = 200,
                Message = "Got stats",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTrainingStaffStats error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}