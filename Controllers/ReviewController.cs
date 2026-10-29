using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the Review table
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private static readonly string RatingError =
        $"Error. Ensure rating is between {Review.MIN_RATING} and {Review.MAX_RATING}.";

    private readonly IMongoCollection<Review> _reviews;

    public ReviewController(MongoDbService mongoDbService)
    {
        _reviews = mongoDbService.Database.GetCollection<Review>("review");
    }

    /// <summary>
    /// gets reviews whose description contains any of the space-separated keywords
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<Review>> Get([FromQuery] bool mostRecent = true, [FromQuery] string? keywords = null)
    {
        var f = Builders<Review>.Filter;
        var words = keywords?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var filter = words.Length == 0
            ? f.Empty
            : f.Or(words.Select(word => f.Regex(r => r.Description, new BsonRegularExpression(Regex.Escape(word), "i"))));

        return await SortByDate(_reviews.Find(filter), mostRecent).ToListAsync();
    }

    /// <summary>
    /// gets specific review with id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> GetById(string id)
    {
        var review = MongoDbService.IsValidId(id) ? await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync() : null;
        return review is null ? NotFound() : Ok(review);
    }

    /// <summary>
    /// adds review entry to table
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Post(Review review)
    {
        if (!IsValidRating(review.Rating))
        {
            return BadRequest(RatingError);
        }

        review.Id = null;
        review.CreatedAt = DateTime.UtcNow;
        review.LikeCount ??= 0;
        review.DislikeCount ??= 0;
        review.Flagged = false;

        await _reviews.InsertOneAsync(review);
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    /// <summary>
    /// updates a review entry
    /// </summary>
    [HttpPut]
    public async Task<ActionResult> Update(Review review)
    {
        if (!IsValidRating(review.Rating))
        {
            return BadRequest(RatingError);
        }

        if (!MongoDbService.IsValidId(review.Id))
        {
            return NotFound();
        }

        var result = await _reviews.ReplaceOneAsync(r => r.Id == review.Id, review);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// deletes a review entry
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var result = await _reviews.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// gets all reviews of a building, most recent first
    /// </summary>
    [HttpGet("building/{buildingId}")]
    public async Task<IEnumerable<Review>> GetByBuildingId(string buildingId) =>
        await SortByDate(_reviews.Find(r => r.BuildingId == buildingId), mostRecent: true).ToListAsync();

    /// <summary>
    /// gets all reviews written by a user, most recent first
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IEnumerable<Review>> GetByUserId(string userId) =>
        await SortByDate(_reviews.Find(r => r.UserId == userId), mostRecent: true).ToListAsync();

    /// <summary>
    /// deletes all reviews of a building
    /// </summary>
    [HttpDelete("building/{buildingId}")]
    public async Task<ActionResult> DeleteByBuildingId(string buildingId)
    {
        await _reviews.DeleteManyAsync(r => r.BuildingId == buildingId);
        return Ok();
    }

    /// <summary>
    /// gets the average rating of a building, or -1 if it has no ratings
    /// </summary>
    [HttpGet("building/average/{buildingId}")]
    public async Task<ActionResult<double>> GetAverageRatingForBuilding(string buildingId)
    {
        var reviews = await _reviews.Find(r => r.BuildingId == buildingId).ToListAsync();
        var ratings = reviews.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value).ToList();
        return Ok(ratings.Count == 0 ? -1 : ratings.Average());
    }

    /// <summary>
    /// marks a review as flagged
    /// </summary>
    [HttpPost("flag")]
    public async Task<ActionResult> FlagReview([FromQuery] string id)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var result = await _reviews.UpdateOneAsync(r => r.Id == id, Builders<Review>.Update.Set(r => r.Flagged, true));
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    private static bool IsValidRating(int? rating) => rating >= Review.MIN_RATING && rating <= Review.MAX_RATING;

    private static IFindFluent<Review, Review> SortByDate(IFindFluent<Review, Review> find, bool mostRecent) =>
        mostRecent ? find.SortByDescending(r => r.CreatedAt) : find.SortBy(r => r.CreatedAt);
}
