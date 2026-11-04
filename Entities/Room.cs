using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class Room
{
    /// <summary>
    /// Room capacity:
    /// <code>
    /// English       | CapacityRangeStart | CapacityRangeEnd
    /// --------------+--------------------+-----------------
    /// 2 to 4 people |                  2 |                4
    /// 2 people      |                  2 |                2
    /// </code>
    /// </summary>
    [BsonElement("capacityRangeStart"), BsonRepresentation(BsonType.Int32)]
    public int CapacityRangeStart { get; set; }

    /// <summary>
    /// Room capacity:
    /// <code>
    /// English       | CapacityRangeStart | CapacityRangeEnd
    /// --------------+--------------------+-----------------
    /// 2 to 4 people |                  2 |                4
    /// 2 people      |                  2 |                2
    /// </code>
    /// </summary>
    [BsonElement("capacityRangeEnd"), BsonRepresentation(BsonType.Int32)]
    public int CapacityRangeEnd { get; set; }

    [BsonElement("features")]
    public List<string> Features { get; set; } = new List<string>();

    [BsonElement("cost"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Cost { get; set; }

    public double HousingRate {get; set; }

    public bool IsSharedBathroom {get; set; }

    [BsonElement("buildingId"), BsonRepresentation(BsonType.ObjectId)]

    public string? BuildingID { get; set; }

}