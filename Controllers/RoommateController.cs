using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the RoommateBio table (issue #87), and compatibility matches.
/// Bios are public to browse, but contactInfo is only filled in for the bio's owner, admins, and students
/// who have an accepted roommate request with the owner.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoommateController : ControllerBase
{
    private static readonly string ScaleError =
        $"Sleep schedule, cleanliness, noise tolerance and guest frequency must each be between "
        + $"{RoommateBio.MIN_SCALE} and {RoommateBio.MAX_SCALE}.";

    private readonly IMongoCollection<RoommateBio> _bios;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<RoommateRequest> _requests;

    public RoommateController(MongoDbService mongoDbService)
    {
        _bios = mongoDbService.Database.GetCollection<RoommateBio>("roommateBio");
        _users = mongoDbService.Database.GetCollection<User>("user");
        _requests = mongoDbService.Database.GetCollection<RoommateRequest>("roommateRequest");
    }

    /// <summary>
    /// gets roommate bios, so users can browse others looking for a roommate.
    /// every filter is optional; gender, major, interest and term ignore case, and an undefined year is ignored.
    /// maxBudget keeps bios whose budget starts at or below it. Bios that aren't looking are left out
    /// unless lookingOnly is false.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<RoommateBio>> Get(
        [FromQuery] string? userId = null,
        [FromQuery] string? gender = null,
        [FromQuery] int? year = null,
        [FromQuery] string? major = null,
        [FromQuery] string? interest = null,
        [FromQuery] string? buildingId = null,
        [FromQuery] string? term = null,
        [FromQuery] int? maxBudget = null,
        [FromQuery] bool lookingOnly = true)
    {
        var f = Builders<RoommateBio>.Filter;
        var filter = f.Empty;

        if (!string.IsNullOrEmpty(userId)) filter &= f.Eq(b => b.UserId, userId);
        if (!string.IsNullOrEmpty(gender)) filter &= f.Regex(b => b.Gender, ExactIgnoreCase(gender));
        if (IsValidYear(year)) filter &= f.Eq(b => b.Year, year);
        if (!string.IsNullOrEmpty(major)) filter &= f.Regex(b => b.Major, ExactIgnoreCase(major));
        // a regex on an array field matches when any element matches
        if (!string.IsNullOrEmpty(interest)) filter &= f.Regex("interests", ExactIgnoreCase(interest));
        if (!string.IsNullOrEmpty(buildingId)) filter &= f.AnyEq(b => b.PreferredBuildingIds, buildingId);
        if (!string.IsNullOrWhiteSpace(term)) filter &= f.Regex(b => b.Term, ExactIgnoreCase(term));
        if (maxBudget.HasValue) filter &= f.Or(f.Eq(b => b.BudgetMin, null), f.Lte(b => b.BudgetMin, maxBudget));
        if (lookingOnly) filter &= IsLooking();

        var bios = await _bios.Find(filter).ToListAsync();
        return await WithVisibleContacts(bios);
    }

    /// <summary>
    /// gets a specific roommate bio by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoommateBio>> GetById(string id)
    {
        var bio = await FindAsync(id);
        return bio is null ? NotFound() : Ok((await WithVisibleContacts(new List<RoommateBio> { bio }))[0]);
    }

    /// <summary>
    /// ranks the other students who are looking by how compatible they are with the signed-in user,
    /// most compatible first. Students who want a different move-in term are left out.
    /// </summary>
    [Authorize]
    [HttpGet("matches")]
    public async Task<ActionResult<IEnumerable<RoommateMatch>>> GetMatches([FromQuery] int limit = 20)
    {
        var userId = User.GetUserId();
        var mine = await _bios.Find(b => b.UserId == userId).FirstOrDefaultAsync();
        if (mine is null)
        {
            return NotFound("Create a roommate bio to see matches.");
        }

        var others = await _bios.Find(IsLooking() & Builders<RoommateBio>.Filter.Ne(b => b.UserId, userId)).ToListAsync();
        var ranked = others
            .Where(other => RoommateMatcher.CanMatch(mine, other))
            .Select(other => (Bio: other, Score: RoommateMatcher.Score(mine, other)))
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.Bio.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToList();

        var bios = await WithVisibleContacts(ranked.Select(m => m.Bio).ToList());
        return Ok(bios.Zip(ranked, (bio, m) => new RoommateMatch(
            bio,
            m.Score,
            RoommateMatcher.SharedBuildings(mine, bio),
            RoommateMatcher.SharedInterests(mine, bio))));
    }

    /// <summary>
    /// creates the signed-in user's roommate bio. Admins can create one for another user by setting userId.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Post(RoommateBio bio)
    {
        var error = Validate(bio);
        if (error != null)
        {
            return BadRequest(error);
        }

        if (!User.IsInRole(nameof(UserType.Admin)) || string.IsNullOrEmpty(bio.UserId))
        {
            bio.UserId = User.GetUserId();
        }

        if (!MongoDbService.IsValidId(bio.UserId))
        {
            return BadRequest("A valid userId is required.");
        }

        if (!await _users.Find(u => u.Id == bio.UserId).AnyAsync())
        {
            return BadRequest("No user exists with that userId.");
        }

        if (await _bios.Find(b => b.UserId == bio.UserId).AnyAsync())
        {
            return Conflict("This user already has a roommate bio.");
        }

        bio.Id = null;
        bio.CreatedAt = DateTime.UtcNow;
        Normalize(bio);
        await _bios.InsertOneAsync(bio);
        return CreatedAtAction(nameof(GetById), new { id = bio.Id }, bio);
    }

    /// <summary>
    /// updates a roommate bio; only its owner or an admin can
    /// </summary>
    [Authorize]
    [HttpPut]
    public async Task<ActionResult> Update(RoommateBio bio)
    {
        var error = Validate(bio);
        if (error != null)
        {
            return BadRequest(error);
        }

        var existing = await FindAsync(bio.Id);
        if (existing is null)
        {
            return NotFound();
        }

        if (!User.IsOwnerOrAdmin(existing.UserId))
        {
            return Forbid();
        }

        // createdAt and userId are set once at creation and can't be changed by the client
        bio.CreatedAt = existing.CreatedAt;
        bio.UserId = existing.UserId;
        Normalize(bio);

        var result = await _bios.ReplaceOneAsync(b => b.Id == bio.Id, bio);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// deletes a roommate bio; only its owner or an admin can
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (!User.IsOwnerOrAdmin(existing.UserId))
        {
            return Forbid();
        }

        await _bios.DeleteOneAsync(b => b.Id == id);
        return Ok();
    }

    private async Task<RoommateBio?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _bios.Find(b => b.Id == id).FirstOrDefaultAsync() : null;

    /// <summary>
    /// clears contactInfo on the bios the signed-in user isn't allowed to see it on
    /// </summary>
    private async Task<List<RoommateBio>> WithVisibleContacts(List<RoommateBio> bios)
    {
        var visible = await RoommateContacts.VisibleToAsync(_requests, User);
        foreach (var bio in bios.Where(b => !RoommateContacts.CanSee(User, visible, b.UserId)))
        {
            bio.ContactInfo = null;
        }

        return bios;
    }

    // bios saved before isLooking existed have no value, and count as looking
    private static FilterDefinition<RoommateBio> IsLooking() => Builders<RoommateBio>.Filter.Ne(b => b.IsLooking, false);

    /// <summary>
    /// returns what's wrong with the bio's fields, or null if they're fine
    /// </summary>
    private static string? Validate(RoommateBio bio)
    {
        if (string.IsNullOrWhiteSpace(bio.Name))
        {
            return "A name is required.";
        }

        if (bio.Age.HasValue && (bio.Age < RoommateBio.MIN_AGE || bio.Age > RoommateBio.MAX_AGE))
        {
            return $"Age must be between {RoommateBio.MIN_AGE} and {RoommateBio.MAX_AGE}.";
        }

        if (bio.Year.HasValue && !IsValidYear(bio.Year))
        {
            return "Invalid year.";
        }

        int?[] scales = { bio.SleepSchedule, bio.Cleanliness, bio.NoiseTolerance, bio.GuestFrequency };
        if (scales.Any(s => s is < RoommateBio.MIN_SCALE or > RoommateBio.MAX_SCALE))
        {
            return ScaleError;
        }

        if (bio.BudgetMin < 0 || bio.BudgetMax < 0 || bio.BudgetMin > bio.BudgetMax)
        {
            return "Budget must be zero or more, with the minimum no higher than the maximum.";
        }

        return null;
    }

    /// <summary>
    /// trims text fields and drops blank or repeated interests and buildings
    /// </summary>
    private static void Normalize(RoommateBio bio)
    {
        bio.Name = bio.Name?.Trim();
        bio.Gender = bio.Gender?.Trim();
        bio.Major = bio.Major?.Trim();
        bio.Term = string.IsNullOrWhiteSpace(bio.Term) ? null : bio.Term.Trim();
        bio.Interests = (bio.Interests ?? new())
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        bio.PreferredBuildingIds = (bio.PreferredBuildingIds ?? new()).Distinct().ToList();
    }

    private static bool IsValidYear(int? year) =>
        year.HasValue && Enum.IsDefined(typeof(ClassYear), year.Value);

    private static BsonRegularExpression ExactIgnoreCase(string value) =>
        new($"^{Regex.Escape(value.Trim())}$", "i");

    /// <summary>
    /// a bio ranked against yours, with a compatibility score from 0 to 100
    /// </summary>
    public record RoommateMatch(RoommateBio Bio, int Compatibility, List<string> SharedBuildings, List<string> SharedInterests);
}
