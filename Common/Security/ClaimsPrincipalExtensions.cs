using System.Security.Claims;

namespace ProjectManager.Common.Security;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
}
