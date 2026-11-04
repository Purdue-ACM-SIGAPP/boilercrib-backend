using System.Security.Claims;
using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static bool IsOwnerOrAdmin(this ClaimsPrincipal user, string? ownerId) =>
        (ownerId != null && ownerId == user.GetUserId()) || user.IsInRole(nameof(UserType.Admin));
}
