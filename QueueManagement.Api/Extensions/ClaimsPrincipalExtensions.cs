using System.Security.Claims;
using QueueManagement.Application.Common.Exceptions;

namespace QueueManagement.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedException(
                "The authenticated user identifier is missing or invalid.");
        }

        return userId;
    }
}
