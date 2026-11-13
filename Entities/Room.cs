using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Room
{
    [BsonElement("id"), BsonRepresentation(BsonType.String)]
    public string? Id { get; set; }

    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }

    [BsonElement("capacity"), BsonRepresentation(BsonType.Int32)]
    public int Capacity { get; set; }

    [BsonElement("features")]
    public List<string> Features { get; set; } = new List<string>();

    [BsonElement("cost"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Cost { get; set; }

    [BsonElement("housingRate"), BsonRepresentation(BsonType.Double)]
    public double HousingRate { get; set; }

    [BsonElement("isSharedBathroom"), BsonRepresentation(BsonType.Boolean)]
    public bool IsSharedBathroom { get; set; }

    [BsonElement("floor"), BsonRepresentation(BsonType.Int32)]
    public int Floor { get; set; }

    [BsonElement("availableUnits"), BsonRepresentation(BsonType.Int32)]
    public int AvailableUnits { get; set; }

    [BsonElement("description"), BsonRepresentation(BsonType.String)]
    public string? Description { get; set; }

    [BsonElement("photos")]
    public List<string> Photos { get; set; } = new List<string>();

    [BsonElement("floorPlan"), BsonRepresentation(BsonType.String)]
    public string? FloorPlan { get; set; }

    [BsonElement("buildingId"), BsonRepresentation(BsonType.String)]
    public string? BuildingID { get; set; }
}