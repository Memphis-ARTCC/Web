using Memphis.API.Data;
using Memphis.API.Services;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.EntityFrameworkCore;

namespace Memphis.API.Extensions;

public static class HttpContextExtensions
{
    public static int? GetCid(this HttpContext httpContext)
    {
        var cidRaw = httpContext.User.Claims.FirstOrDefault(x => x.Type == "cid")?.Value;
        if (cidRaw == null)
            return null;

        var goodCid = int.TryParse(cidRaw, out var cid);
        return goodCid ? cid : null;
    }

    public static string? GetName(this HttpContext httpContext)
    {
        return httpContext.User.Claims.FirstOrDefault(x => x.Type == "fullName")?.Value;
    }

    public static string? GetEmail(this HttpContext httpContext)
    {
        return httpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
    }

    public static async Task<bool> IsMember(this HttpContext httpContext, DatabaseContext context)
    {
        var cidRaw = httpContext.User.Claims.FirstOrDefault(x => x.Type == "cid")?.Value;
        if (cidRaw == null)
            return false;

        var goodCid = int.TryParse(cidRaw, out var cid);
        if (!goodCid)
            return false;

        return await context.Users.AnyAsync(x => x.Id == cid);
    }

    public static async Task<User?> GetUser(this HttpContext httpContext, DatabaseContext context)
    {
        var cidRaw = httpContext.User.Claims.FirstOrDefault(x => x.Type == "cid")?.Value;
        if (cidRaw == null)
            return null;

        var goodCid = int.TryParse(cidRaw, out var cid);
        if (!goodCid)
            return null;

        var user = await context.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == cid);
        return user;
    }

    public static async Task<bool> IsTrainingStaff(this HttpContext httpContext, RedisService redisService)
    {
        return await redisService.ValidateRoles(httpContext.User, Constants.TrainingStaffList);
    }

    public static async Task<bool> IsAllStaff(this HttpContext httpContext, RedisService redisService)
    {
        return await redisService.ValidateRoles(httpContext.User, Constants.AllStaffList);
    }

    public static async Task<bool> IsFullStaff(this HttpContext httpContext, RedisService redisService)
    {
        return await redisService.ValidateRoles(httpContext.User, Constants.FullStaffList);
    }

    public static async Task<bool> IsSeniorStaff(this HttpContext httpContext, RedisService redisService)
    {
        return await redisService.ValidateRoles(httpContext.User, Constants.SeniorStaffList);
    }
}