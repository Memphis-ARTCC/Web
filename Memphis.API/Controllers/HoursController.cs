using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.Shared.Dtos;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HoursController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<HoursController> _logger;

    public HoursController(DatabaseContext context, ISentryClient sentryHub, ILogger<HoursController> logger)
    {
        _context = context;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<HoursDto>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<IList<HoursDto>>> GetHours(int? month, int? year)
    {
        try
        {
            month ??= DateTimeOffset.UtcNow.Month;
            year ??= DateTimeOffset.UtcNow.Year;

            var hours = await _context.Hours.Include(x => x.User)
                .Include(x => x.User.Roles).Where(x => x.Month == month && x.Year == year)
                .OrderBy(x => x.User.LastName).ToListAsync();

            var result = new List<HoursDto>();
            foreach (var entry in hours)
            {
                var userEntry = RosterUserDto.Parse(entry.User);
                result.Add(new HoursDto
                {
                    Month = entry.Month,
                    Year = entry.Year,
                    User = userEntry,
                    DeliveryHours = entry.DeliveryHours,
                    GroundHours = entry.GroundHours,
                    TowerHours = entry.TowerHours,
                    ApproachHours = entry.ApproachHours,
                    CenterHours = entry.CenterHours
                });
            }

            // Get all users who didn't get any hours so create a HoursDto wih 0 hours for each
            var userIds = result.Select(x => x.User.Cid).ToList();
            var usersNoHours = await _context.Users.Include(x => x.Roles)
                .Where(x => !userIds.Contains(x.Id)).ToListAsync();

            foreach (var entry in usersNoHours)
            {
                var userEntry = RosterUserDto.Parse(entry);
                result.Add(new HoursDto
                {
                    Month = month ?? 0,
                    Year = year ?? 0,
                    User = userEntry,
                    DeliveryHours = 0.0,
                    GroundHours = 0.0,
                    TowerHours = 0.0,
                    ApproachHours = 0.0,
                    CenterHours = 0.0
                });
            }

            return Ok(new Response<IList<HoursDto>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} hours entries",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetHours error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{cid:int}")]
    [ProducesResponseType(typeof(Response<IList<HoursDto>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<HoursDto>>>> GetUserHours(int cid)
    {
        try
        {
            var user = await _context.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == cid);
            if (user == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"User '{cid}' not found"
                });
            }

            var result = new List<HoursDto>();
            var now = DateTimeOffset.UtcNow;
            var userEntry = RosterUserDto.Parse(user);
            for (var i = 0; i < 12; i++)
            {
                var hours = await _context.Hours.Include(x => x.User).Include(x => x.User.Roles)
                    .Where(x => x.User == user && x.Month == now.Month && x.Year == now.Year).FirstOrDefaultAsync();
                if (hours == null)
                {
                    result.Add(new HoursDto
                    {
                        Month = now.Month,
                        Year = now.Year,
                        User = userEntry,
                        DeliveryHours = 0.0,
                        GroundHours = 0.0,
                        TowerHours = 0.0,
                        ApproachHours = 0.0,
                        CenterHours = 0.0
                    });
                    now = now.AddMonths(-1);
                    continue;
                }

                result.Add(new HoursDto
                {
                    Month = now.Month,
                    Year = now.Year,
                    User = userEntry,
                    DeliveryHours = hours.DeliveryHours,
                    GroundHours = hours.GroundHours,
                    TowerHours = hours.TowerHours,
                    ApproachHours = hours.ApproachHours,
                    CenterHours = hours.CenterHours
                });
                now = now.AddMonths(-1);
            }

            return Ok(new Response<IList<HoursDto>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} hours entries",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserHours error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}