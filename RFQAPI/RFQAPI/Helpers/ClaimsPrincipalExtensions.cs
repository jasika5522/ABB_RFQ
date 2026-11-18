using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RFQAPI.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("user_id")
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);

        if (claim is null || !long.TryParse(claim.Value, out var userId))
        {
            throw new ApiException("Invalid token.", StatusCodes.Status401Unauthorized);
        }

        return userId;
    }
}