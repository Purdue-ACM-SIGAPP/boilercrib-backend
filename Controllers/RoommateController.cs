using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the RoommateBio table (issue #87)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoommateController : ControllerBase
{
    private readonly IMongoCollection<RoommateBio> _bios;
    private readonly IMongoCollection<User> _users;

    public RoommateController(MongoDbService mongoDbService)
    {
        _bios = mongoDbService.Database.GetCollection<RoommateBio>("roommateBio");
        _users = mongoDbService.Database.GetCollection<User>("user");
    }

    /// <summary>
    /// gets roommate bios, so users can browse others looking for a roommate.
    /// every filter is optional; gender, major, and interest ignore case, and an undefined year is ignored
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<RoommateBio>> Get(
        [FromQuery] string? userId = null,
        [FromQuery] string? gender = null,
        [FromQuery] int? year = null,
        [FromQuery] string? major = null,
        [FromQuery] string? interest = null)
    {
        var f = Builders<RoommateBio>.Filter;
        var filter = f.Empty;

        if (!string.IsNullOrEmpty(userId)) filter &= f.Eq(b => b.UserId, userId);
        if (!string.IsNullOrEmpty(gender)) filter &= f.Regex(b => b.Gender, ExactIgnoreCase(gender));
        if (IsValidYear(year)) filter &= f.Eq(b => b.Year, year);
        if (!string.IsNullOrEmpty(major)) filter &= f.Regex(b => b.Major, ExactIgnoreCase(major));
        // a regex on an array field matches when any element matches
        if (!string.IsNullOrEmpty(interest)) filter &= f.Regex("interests", ExactIgnoreCase(interest));

        return await _bios.Find(filter).ToListAsync();
    }

    /// <summary>
    /// gets a specific roommate bio by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoommateBio>> GetById(string id)
    {
        var bio = MongoDbService.IsValidId(id)
            ? await _bios.Find(b => b.Id == id).FirstOrDefaultAsync()
            : null;
        return bio is null ? NotFound() : Ok(bio);
    }

    /// <summary>
    /// creates a roommate bio for a user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Post(RoommateBio bio)
    {
        var error = Validate(bio);
        if (error != null)
        {
            return BadRequest(error);
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
        Normalize(bio);
        await _bios.InsertOneAsync(bio);
        return CreatedAtAction(nameof(GetById), new { id = bio.Id }, bio);
    }

    /// <summary>
    /// updates a roommate bio
    /// </summary>
    [HttpPut]
    public async Task<ActionResult> Update(RoommateBio bio)
    {
        var error = Validate(bio);
        if (error != null)
        {
            return BadRequest(error);
        }

        if (!MongoDbService.IsValidId(bio.Id))
        {
            return NotFound();
        }

        var existing = await _bios.Find(b => b.Id == bio.Id).FirstOrDefaultAsync();
        if (existing is null)
        {
            return NotFound();
        }

        // createdAt and userId are set once at creation and can't be changed by the client
        bio.CreatedAt = existing.CreatedAt;
        bio.UserId = existing.UserId;
        Normalize(bio);

        var result = await _bios.ReplaceOneAsync(b => b.Id == bio.Id, bio);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// deletes a roommate bio
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var result = await _bios.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount > 0 ? Ok() : NotFound();
    }

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

        return null;
    }

    /// <summary>
    /// trims text fields and drops blank or repeated interests
    /// </summary>
    private static void Normalize(RoommateBio bio)
    {
        bio.Name = bio.Name?.Trim();
        bio.Gender = bio.Gender?.Trim();
        bio.Major = bio.Major?.Trim();
        bio.Interests = (bio.Interests ?? new())
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsValidYear(int? year) =>
        year.HasValue && Enum.IsDefined(typeof(ClassYear), year.Value);

    private static BsonRegularExpression ExactIgnoreCase(string value) =>
        new($"^{Regex.Escape(value.Trim())}$", "i");
}
