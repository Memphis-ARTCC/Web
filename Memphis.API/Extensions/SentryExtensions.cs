using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace Memphis.API.Extensions;

public static class SentryExtensions
{
    public static ActionResult ReturnActionResult(this SentryId id)
    {
        return new ObjectResult(new Response<Guid?>
        {
            StatusCode = 500,
            Message = "An error has occurred",
            Data = id
        })
        { StatusCode = 500 };
    }
}