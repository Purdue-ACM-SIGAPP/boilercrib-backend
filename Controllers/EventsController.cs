using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the Events table
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IMongoCollection<Events> _events;

    public EventsController(MongoDbService mongoDbService)
    {
        _events = mongoDbService.Database.GetCollection<Events>("events");
    }

    /// <summary>
    /// gets events matching the optional filters, soonest first; date matches the whole day
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<Events>> Get(
        [FromQuery] string? eventName = null,
        [FromQuery] string? summary = null,
        [FromQuery] string? content = null,
        [FromQuery] string? userID = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] string? address = null)
    {
        var f = Builders<Events>.Filter;
        var filter = f.Empty;

        if (!string.IsNullOrEmpty(eventName)) filter &= f.Eq(e => e.EventName, eventName);
        if (!string.IsNullOrEmpty(summary)) filter &= f.Eq(e => e.Summary, summary);
        if (!string.IsNullOrEmpty(content)) filter &= f.Eq(e => e.Content, content);
        if (!string.IsNullOrEmpty(userID)) filter &= f.Eq(e => e.UserID, userID);
        if (!string.IsNullOrEmpty(address)) filter &= f.Eq(e => e.Address, address);
        if (date.HasValue)
        {
            filter &= f.Gte(e => e.Date, (DateTime?)date.Value.Date) & f.Lt(e => e.Date, (DateTime?)date.Value.Date.AddDays(1));
        }

        return await _events.Find(filter).SortBy(e => e.Date).ToListAsync();
    }

    /// <summary>
    /// gets specific event with id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Events>> GetById(string id)
    {
        var evt = await FindAsync(id);
        return evt is null ? NotFound() : Ok(evt);
    }

    /// <summary>
    /// adds an event
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Post(Events evt)
    {
        evt.Id = null;
        evt.UserID = User.GetUserId();
        await _events.InsertOneAsync(evt);
        return CreatedAtAction(nameof(GetById), new { id = evt.Id }, evt);
    }

    /// <summary>
    /// updates an event, keeping its original author
    /// </summary>
    [Authorize]
    [HttpPut]
    public async Task<ActionResult> Update(Events evt)
    {
        var existing = await FindAsync(evt.Id);
        if (existing is null)
        {
            return NotFound();
        }

        if (!User.IsOwnerOrAdmin(existing.UserID))
        {
            return Forbid();
        }

        evt.UserID = existing.UserID;
        await _events.ReplaceOneAsync(e => e.Id == evt.Id, evt);
        return Ok();
    }

    /// <summary>
    /// deletes an event
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

        if (!User.IsOwnerOrAdmin(existing.UserID))
        {
            return Forbid();
        }

        await _events.DeleteOneAsync(e => e.Id == id);
        return Ok();
    }

    private async Task<Events?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _events.Find(e => e.Id == id).FirstOrDefaultAsync() : null;
}
