using Memphis.API.Data;
using Memphis.Shared.Models;

namespace Memphis.API.Services;

public class LoggingService
{
    private readonly DatabaseContext _context;

    public LoggingService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task AddWebsiteLog(HttpRequest request, string action, string oldData, string newData)
    {
        var ip = request.Headers["CF-Connecting-IP"].ToString() ??
                 request.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Not Found";
        var cid = request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "cid")?.Value ?? "Not Found";
        var name = request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "fullName")?.Value ?? "Not Found";
        await _context.WebsiteLogs.AddAsync(new WebsiteLog
        {
            Ip = ip == "::1" ? "localhost" : ip,
            Cid = cid,
            Name = name,
            Action = action,
            OldData = oldData,
            NewData = newData
        });
        await _context.SaveChangesAsync();
    }
}