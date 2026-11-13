using FuzzySharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the Building table
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BuildingController : ControllerBase
{
    private enum FilterMatch { AtLeast, Equal, Contains }

    private const int MaxImageBytes = 5 * 1024 * 1024;

    // Filterable fields, keyed by criteria name (case-insensitive), and the building type they apply to
    private static readonly Dictionary<string, (string Type, string Field, FilterMatch Match)> FilterFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pianoNum"] = (nameof(Housing), "pianoNum", FilterMatch.AtLeast),
            ["kitchenNum"] = (nameof(Housing), "kitchenNum", FilterMatch.AtLeast),
            ["studySpaceNum"] = (nameof(Housing), "studySpaceNum", FilterMatch.AtLeast),
            ["haveDinningCourt"] = (nameof(Housing), "haveDinningCourt", FilterMatch.Equal),
            ["haveBoilerMarket"] = (nameof(Housing), "haveBoilerMarket", FilterMatch.Equal),
            ["acceptsSwipes"] = (nameof(DinningCourt), "acceptsSwipes", FilterMatch.Equal),
            ["acceptsDiningDollars"] = (nameof(DinningCourt), "acceptsDiningDollars", FilterMatch.Equal),
            ["acceptsBoilerExpress"] = (nameof(DinningCourt), "acceptsBoilerExpress", FilterMatch.Equal),
            ["stableOptions"] = (nameof(DinningCourt), "stableOptions", FilterMatch.Contains),
            ["busyHours"] = (nameof(DinningCourt), "busyHours", FilterMatch.Contains),
        };

    private readonly ILogger<BuildingController> _logger;
    private readonly IMongoCollection<Building> _buildings;
    private readonly IMongoCollection<Housing> _housing;
    private readonly IMongoCollection<Review> _reviews;
    private readonly BuildingOutlineService _outlineService;
    private readonly GoogleMapsService _mapsService;

    public BuildingController(ILogger<BuildingController> logger, MongoDbService mongoDbService,
        BuildingOutlineService outlineService, GoogleMapsService mapsService)
    {
        _logger = logger;
        _buildings = mongoDbService.Database.GetCollection<Building>("building");
        _housing = mongoDbService.Database.GetCollection<Housing>("building");
        _reviews = mongoDbService.Database.GetCollection<Review>("review");
        _outlineService = outlineService;
        _mapsService = mapsService;
    }

    /// <summary>
    /// gets buildings, best approximate match for query first when one is given
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<Building>> Get(
        [FromQuery] string? query = null,
        [FromQuery] int? scoreThreshold = null,
        [FromQuery] int? pageLength = null,
        [FromQuery] int? pageIndex = null)
    {
        IEnumerable<Building> buildings = await _buildings.Find(FilterDefinition<Building>.Empty).ToListAsync();

        if (!string.IsNullOrEmpty(query))
        {
            int Score(string? value) => value == null ? 0 : Fuzz.WeightedRatio(query, value);

            var scored = buildings
                .Select(b => (Building: b, Score: Math.Max(Score(b.Name), Score(b.Acronym))))
                .OrderByDescending(x => x.Score);
            buildings = (scoreThreshold is > 0 ? scored.Where(x => x.Score >= scoreThreshold) : scored)
                .Select(x => x.Building);
        }

        if (pageLength is > 0)
        {
            buildings = pageIndex < 0
                ? Enumerable.Empty<Building>()
                : buildings.Skip((pageIndex ?? 0) * pageLength.Value).Take(pageLength.Value);
        }

        return buildings.ToList();
    }

    /// <summary>
    /// gets specific building with id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Building>> GetById(string id)
    {
        var building = await FindAsync(id);
        return building is null ? NotFound() : Ok(building);
    }

    /// <summary>
    /// Gets a complete, client-ready housing overview for a housing building.
    /// Existing building endpoints remain unchanged for compatibility.
    /// </summary>
    [HttpGet("{id}/housing")]
    public async Task<ActionResult<HousingDetailResponse>> GetHousingDetails(string id)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var housing = await _housing.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (housing is null || !string.Equals(housing.BuildingType, "Housing", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var reviews = await _reviews
            .Find(r => r.BuildingId == id)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();

        var roomOptions = housing.Rooms ?? new List<Room>();
        var availableListings = roomOptions
            .Where(room => room.AvailableUnits > 0)
            .ToList();

        var response = new HousingDetailResponse
        {
            Id = housing.Id,
            Name = housing.Name,
            Acronym = housing.Acronym,
            Address = housing.Address,
            Latitude = housing.Latitude,
            Longitude = housing.Longitude,
            Image = housing.Image,
            BuildingType = housing.BuildingType,
            Description = housing.Description,
            Contact = new HousingContact
            {
                Phone = housing.ContactPhone,
                Email = housing.ContactEmail,
                Website = housing.ContactWebsite,
            },
            Pricing = roomOptions.Select(room => room.Cost).Where(cost => cost > 0).ToList(),
            RoomOptions = roomOptions,
            AvailableListings = availableListings,
            FloorPlans = housing.FloorPlans,
            Photos = housing.Photos,
            Reviews = reviews,
            RoomCount = roomOptions.Count,
            AveragePrice = roomOptions.Count == 0
                ? null
                : roomOptions.Where(room => room.Cost > 0).Select(room => room.Cost).Average(),
        };

        return Ok(response);
    }

    /// <summary>
    /// adds building entry to table, geocoding its address when coordinates are missing
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpPost]
    public async Task<ActionResult> Post(Building building)
    {
        building.Id = null;

        if ((building.Latitude is null || building.Longitude is null) && !string.IsNullOrEmpty(building.Address))
        {
            var location = await _mapsService.GeocodeAsync(building.Address);
            if (location is null)
            {
                return UnprocessableEntity(new { message = "Could not get coordinates from the address." });
            }

            building.Latitude = location.Value.Latitude;
            building.Longitude = location.Value.Longitude;
        }

        await _buildings.InsertOneAsync(building);
        return CreatedAtAction(nameof(GetById), new { id = building.Id }, building);
    }

    /// <summary>
    /// updates a building entry
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpPut]
    public async Task<ActionResult> Update(Building building)
    {
        if (!MongoDbService.IsValidId(building.Id))
        {
            return NotFound();
        }

        var result = await _buildings.ReplaceOneAsync(b => b.Id == building.Id, building);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// deletes a building entry
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var result = await _buildings.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// gets outlines of the OpenStreetMap buildings within radius (degrees) of a building
    /// </summary>
    [HttpGet("outline")]
    public async Task<ActionResult> GetOutline([FromQuery] string id, [FromQuery] double radius = 0.001) // ~100m
    {
        var building = await FindAsync(id);
        if (building is null)
        {
            return NotFound($"Building with ID {id} not found.");
        }

        if (building.Latitude is null || building.Longitude is null)
        {
            return UnprocessableEntity(new { message = "Building has no coordinates." });
        }

        try
        {
            var outlines = await _outlineService.GetBuildingOutline(building.Latitude.Value, building.Longitude.Value, radius);
            return Ok(outlines.Select(outline => new
            {
                buildingID = id,
                coordinates = outline.Select(point => new { latitude = point.Lat, longitude = point.Lon })
            }));
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Overpass request failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Error while fetching building outlines." });
        }
    }

    /// <summary>
    /// filters buildings by type and a JSON object of type-specific criteria, e.g. {"pianoNum": 1}
    /// </summary>
    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<Building>>> Filter([FromQuery] string type, [FromQuery] string? criteria)
    {
        var filter = Builders<Building>.Filter.Eq(b => b.BuildingType, type);

        if (!string.IsNullOrEmpty(criteria))
        {
            try
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(criteria) ?? new();
                foreach (var (key, value) in values)
                {
                    if (!FilterFields.TryGetValue(key, out var field) || field.Type != type)
                    {
                        continue;
                    }

                    filter &= field.Match switch
                    {
                        FilterMatch.AtLeast => Builders<Building>.Filter.Gte(field.Field, Convert.ToInt32(value)),
                        FilterMatch.Equal => Builders<Building>.Filter.Eq(field.Field, Convert.ToBoolean(value)),
                        _ => Builders<Building>.Filter.AnyEq(field.Field, value?.ToString()),
                    };
                }
            }
            catch (Exception e) when (e is JsonException or FormatException or InvalidCastException or OverflowException)
            {
                return BadRequest("Invalid criteria format.");
            }
        }

        return Ok(await _buildings.Find(filter).ToListAsync());
    }

    /// <summary>
    /// sets a building's image from an uploaded file, stored as base64
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpPost("uploadImage/{id}")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult> UploadImage(string id, IFormFile file)
    {
        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var image = stream.ToArray();
        if (!IsJpegOrPng(image))
        {
            return BadRequest("Upload a JPEG or PNG image.");
        }

        var update = Builders<Building>.Update.Set(b => b.Image, Convert.ToBase64String(image));
        var result = await _buildings.UpdateOneAsync(b => b.Id == id, update);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    private async Task<Building?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _buildings.Find(b => b.Id == id).FirstOrDefaultAsync() : null;

    private static bool IsJpegOrPng(byte[] bytes) =>
        bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })
        || bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
}
