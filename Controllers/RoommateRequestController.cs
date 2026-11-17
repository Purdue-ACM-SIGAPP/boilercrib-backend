using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for roommate requests between students. Every endpoint acts as the signed-in user.
/// Once a request is accepted, both students can see each other's phone number and roommate bio contactInfo.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoommateRequestController : ControllerBase
{
    private const int MaxMessageLength = 500;

    private readonly IMongoCollection<RoommateRequest> _requests;
    private readonly IMongoCollection<RoommateBio> _bios;
    private readonly IMongoCollection<User> _users;

    public RoommateRequestController(MongoDbService mongoDbService)
    {
        _requests = mongoDbService.Database.GetCollection<RoommateRequest>("roommateRequest");
        _bios = mongoDbService.Database.GetCollection<RoommateBio>("roommateBio");
        _users = mongoDbService.Database.GetCollection<User>("user");
    }

    /// <summary>
    /// gets the requests the signed-in user has received and sent, newest first, each with the other
    /// student's name. Their contact info is included only on accepted requests. Pass a status to see
    /// only those requests.
    /// </summary>
    [HttpGet]
    public async Task<UserRequests> Get([FromQuery] RoommateRequestStatus? status = null)
    {
        var userId = User.GetUserId();
        var f = Builders<RoommateRequest>.Filter;
        var filter = f.Or(f.Eq(r => r.ToUserId, userId), f.Eq(r => r.FromUserId, userId));
        if (status.HasValue) filter &= f.Eq(r => r.Status, status.Value);

        var requests = await _requests.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync();
        string? OtherId(RoommateRequest r) => r.ToUserId == userId ? r.FromUserId : r.ToUserId;

        var otherIds = requests.Select(OtherId).OfType<string>().Distinct().ToList();
        var users = (await _users.Find(Builders<User>.Filter.In(u => u.Id, otherIds.Where(MongoDbService.IsValidId)))
                .ToListAsync()).ToDictionary(u => u.Id!);
        var bios = (await _bios.Find(Builders<RoommateBio>.Filter.In(b => b.UserId, otherIds)).ToListAsync())
            .GroupBy(b => b.UserId!).ToDictionary(g => g.Key, g => g.First());

        RequestView View(RoommateRequest r)
        {
            var otherId = OtherId(r);
            var user = otherId is null ? null : users.GetValueOrDefault(otherId);
            var bio = otherId is null ? null : bios.GetValueOrDefault(otherId);
            var contact = r.Status == RoommateRequestStatus.Accepted
                ? new Contact(user?.PhoneNumber, bio?.ContactInfo)
                : null;
            return new RequestView(r, new OtherStudent(otherId, user?.Username, bio?.Name ?? user?.Name, bio?.Id), contact);
        }

        return new UserRequests(
            requests.Where(r => r.ToUserId == userId).Select(View).ToList(),
            requests.Where(r => r.FromUserId == userId).Select(View).ToList());
    }

    /// <summary>
    /// sends a roommate request from the signed-in user. Both students need a roommate bio, the recipient
    /// must still be looking, and there can be only one pending request between two students.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Send(SendRequest body)
    {
        var fromUserId = User.GetUserId();
        if (string.IsNullOrEmpty(body.ToUserId))
        {
            return BadRequest("toUserId is required.");
        }

        if (body.ToUserId == fromUserId)
        {
            return BadRequest("You can't send a roommate request to yourself.");
        }

        if (body.Message?.Length > MaxMessageLength)
        {
            return BadRequest($"Keep the message under {MaxMessageLength} characters.");
        }

        if (!await _bios.Find(b => b.UserId == fromUserId).AnyAsync())
        {
            return BadRequest("Create a roommate bio before sending requests.");
        }

        var recipient = await _bios.Find(b => b.UserId == body.ToUserId).FirstOrDefaultAsync();
        if (recipient is null || !recipient.IsLooking)
        {
            return BadRequest("That student isn't looking for a roommate.");
        }

        var f = Builders<RoommateRequest>.Filter;
        var betweenThem = f.Or(
            f.And(f.Eq(r => r.FromUserId, fromUserId), f.Eq(r => r.ToUserId, body.ToUserId)),
            f.And(f.Eq(r => r.FromUserId, body.ToUserId), f.Eq(r => r.ToUserId, fromUserId)));
        if (await _requests.Find(betweenThem & f.Eq(r => r.Status, RoommateRequestStatus.Pending)).AnyAsync())
        {
            return Conflict("There's already a pending request between you two.");
        }

        var request = new RoommateRequest
        {
            FromUserId = fromUserId,
            ToUserId = body.ToUserId,
            Message = string.IsNullOrWhiteSpace(body.Message) ? null : body.Message.Trim(),
            Status = RoommateRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        await _requests.InsertOneAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    /// <summary>
    /// gets a specific request; only the two students in it, or an admin, can
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoommateRequest>> GetById(string id)
    {
        var request = await FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        return User.IsOwnerOrAdmin(request.FromUserId) || User.IsOwnerOrAdmin(request.ToUserId) ? Ok(request) : Forbid();
    }

    /// <summary>
    /// the recipient accepts a pending request. Both students' bios stop showing as looking;
    /// either can turn that back on by updating their bio.
    /// </summary>
    [HttpPost("{id}/accept")]
    public async Task<ActionResult> Accept(string id)
    {
        var (request, error) = await PendingForRecipient(id);
        if (request is null)
        {
            return error!;
        }

        if (!await Respond(id, RoommateRequestStatus.Accepted))
        {
            return Conflict("That request was already answered.");
        }

        var pair = new[] { request.FromUserId, request.ToUserId };
        await _bios.UpdateManyAsync(
            Builders<RoommateBio>.Filter.In(b => b.UserId, pair),
            Builders<RoommateBio>.Update.Set(b => b.IsLooking, false));
        return Ok();
    }

    /// <summary>
    /// the recipient declines a pending request
    /// </summary>
    [HttpPost("{id}/decline")]
    public async Task<ActionResult> Decline(string id)
    {
        var (request, error) = await PendingForRecipient(id);
        if (request is null)
        {
            return error!;
        }

        return await Respond(id, RoommateRequestStatus.Declined) ? Ok() : Conflict("That request was already answered.");
    }

    /// <summary>
    /// the sender withdraws a request that hasn't been answered yet
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Cancel(string id)
    {
        var request = await FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.FromUserId != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Only the student who sent a request can cancel it.");
        }

        var result = await _requests.DeleteOneAsync(r => r.Id == id && r.Status == RoommateRequestStatus.Pending);
        return result.DeletedCount > 0 ? Ok() : Conflict("That request was already answered.");
    }

    private async Task<RoommateRequest?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _requests.Find(r => r.Id == id).FirstOrDefaultAsync() : null;

    private async Task<(RoommateRequest? Request, ActionResult? Error)> PendingForRecipient(string id)
    {
        var request = await FindAsync(id);
        if (request is null)
        {
            return (null, NotFound());
        }

        if (request.ToUserId != User.GetUserId())
        {
            return (null, StatusCode(StatusCodes.Status403Forbidden, "Only the student who received a request can answer it."));
        }

        return request.Status == RoommateRequestStatus.Pending
            ? (request, null)
            : (null, Conflict("That request was already answered."));
    }

    // only changes a request that is still pending, so two answers can't both go through
    private async Task<bool> Respond(string id, RoommateRequestStatus status)
    {
        var result = await _requests.UpdateOneAsync(
            r => r.Id == id && r.Status == RoommateRequestStatus.Pending,
            Builders<RoommateRequest>.Update.Set(r => r.Status, status).Set(r => r.RespondedAt, DateTime.UtcNow));
        return result.ModifiedCount > 0;
    }

    public record SendRequest(string? ToUserId, string? Message);

    /// <summary>
    /// the other student in a request: their user id, username, name, and roommate bio id
    /// </summary>
    public record OtherStudent(string? UserId, string? Username, string? Name, string? BioId);

    /// <summary>
    /// how to reach the other student; only sent once a request is accepted
    /// </summary>
    public record Contact(string? PhoneNumber, string? ContactInfo);

    public record RequestView(RoommateRequest Request, OtherStudent OtherStudent, Contact? Contact);

    public record UserRequests(List<RequestView> Received, List<RequestView> Sent);
}
