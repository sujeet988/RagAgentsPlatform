using System.Security.Claims;

namespace RagAgents.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("oid")
                ?? user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        }

        public static string? GetUserEmail(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("preferred_username")
                ?? user.FindFirstValue(ClaimTypes.Upn)
                ?? user.FindFirstValue("upn")
                ?? user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue("email");
        }

        public static string? GetDisplayName(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("name");
        }
    }
}
