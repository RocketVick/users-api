using System.Security.Claims;

namespace TNG.Users.API.Extensions;

public static class HttpContextExtensions
{
    public static int? GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim == null ? null : int.Parse(userIdClaim.Value);
    }
}