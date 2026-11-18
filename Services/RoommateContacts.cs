using System.Security.Claims;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

/// <summary>
/// Decides who may see a student's contact info (phone number and roommate bio contactInfo):
/// the student, admins, and students they have an accepted roommate request with.
/// </summary>
public static class RoommateContacts
{
    /// <summary>
    /// ids of the students the viewer can see contact info for, besides their own
    /// </summary>
    public static async Task<HashSet<string>> VisibleToAsync(
        IMongoCollection<RoommateRequest> requests, ClaimsPrincipal viewer)
    {
        var viewerId = viewer.GetUserId();
        if (viewerId is null)
        {
            return new HashSet<string>();
        }

        var f = Builders<RoommateRequest>.Filter;
        var accepted = await requests
            .Find(f.Eq(r => r.Status, RoommateRequestStatus.Accepted)
                  & f.Or(f.Eq(r => r.FromUserId, viewerId), f.Eq(r => r.ToUserId, viewerId)))
            .ToListAsync();

        var visible = accepted.Select(r => r.FromUserId == viewerId ? r.ToUserId : r.FromUserId)
            .OfType<string>().ToHashSet();
        visible.Add(viewerId);
        return visible;
    }

    public static bool CanSee(ClaimsPrincipal viewer, HashSet<string> visible, string? userId) =>
        viewer.IsInRole(nameof(UserType.Admin)) || (userId != null && visible.Contains(userId));
}
