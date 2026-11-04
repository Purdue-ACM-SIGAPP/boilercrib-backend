using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Distance and geocoding endpoints backed by Google Maps
/// </summary>
[ApiController]
[Route("api/maps")]
public class MapsController : ControllerBase
{
    private readonly IMongoCollection<Building> _buildings;
    private readonly GoogleMapsService _mapsService;

    public MapsController(MongoDbService mongoDbService, GoogleMapsService mapsService)
    {
        _buildings = mongoDbService.Database.GetCollection<Building>("building");
        _mapsService = mapsService;
    }

    /// <summary>
    /// gets travel distance and duration from a coordinate to a building
    /// </summary>
    [HttpGet("user_distance")]
    public async Task<IActionResult> GetUserDistance(string buildingId, double latitude, double longitude)
    {
        var building = await FindAsync(buildingId);
        if (building is null)
        {
            return NotFound(new { message = "Building not found." });
        }

        if (string.IsNullOrEmpty(building.Address))
        {
            return BadRequest(new { message = "Address is not set for building" });
        }

        var result = await _mapsService.GetDistanceAsync(FormattableString.Invariant($"{latitude},{longitude}"), building.Address);
        return result is null
            ? UnprocessableEntity(new { message = "Could not calculate distance or duration." })
            : Ok(new { distance = result.Value.Distance, duration = result.Value.Duration });
    }

    /// <summary>
    /// gets travel distance and duration between two buildings
    /// </summary>
    [HttpGet("distance")]
    public async Task<IActionResult> GetDistance(string buildingId1, string buildingId2)
    {
        var building1 = await FindAsync(buildingId1);
        var building2 = await FindAsync(buildingId2);
        if (building1 is null || building2 is null)
        {
            return NotFound(new { message = "One or both buildings not found." });
        }

        if (string.IsNullOrEmpty(building1.Address) || string.IsNullOrEmpty(building2.Address))
        {
            return BadRequest(new { message = "Addresses are missing for one or both buildings." });
        }

        var result = await _mapsService.GetDistanceAsync(building1.Address, building2.Address);
        return result is null
            ? UnprocessableEntity(new { message = "Could not calculate distance or duration." })
            : Ok(new
            {
                building1 = building1.Name,
                building2 = building2.Name,
                distance = result.Value.Distance,
                duration = result.Value.Duration
            });
    }

    /// <summary>
    /// sets a building's coordinates by geocoding its address
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpPost("set-coordinates")]
    public async Task<IActionResult> SetCoordinates(string buildingId)
    {
        var building = await FindAsync(buildingId);
        if (building is null)
        {
            return NotFound(new { message = "Building not found." });
        }

        if (string.IsNullOrEmpty(building.Address))
        {
            return BadRequest(new { message = "Address is missing for the building." });
        }

        var location = await _mapsService.GeocodeAsync(building.Address);
        if (location is null)
        {
            return UnprocessableEntity(new { message = "Could not get coordinates from the address." });
        }

        var (latitude, longitude) = location.Value;
        var update = Builders<Building>.Update.Set(b => b.Latitude, latitude).Set(b => b.Longitude, longitude);
        await _buildings.UpdateOneAsync(b => b.Id == buildingId, update);

        return Ok(new { message = "Coordinates updated successfully.", buildingId, latitude, longitude });
    }

    private async Task<Building?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _buildings.Find(b => b.Id == id).FirstOrDefaultAsync() : null;
}
