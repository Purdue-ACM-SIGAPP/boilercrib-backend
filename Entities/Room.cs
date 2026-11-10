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

    /// <summary>
    /// What the capacity is for, e.g. per "Room," or per "Bedroom"
    /// Useful in the case of multiple rooms or bedrooms
    /// </summary>
    [BsonElement("capacityPer"), BsonRepresentation(BsonType.String)]
    public string? CapacityPer { get; set; }

    /// <summary>
    /// Type of room, e.g. "Triple w/shrd bath," "2 Rm Suite for 4 priv bth," "Double"
    /// </summary>
    [BsonElement("roomType"), BsonRepresentation(BsonType.String)]
    public string? RoomType { get; set; }

    [BsonElement("features"), BsonRepresentation(BsonType.Array)]
    public List<string> Features { get; set; } = new List<string>();

    /// <summary>
    /// Cost per period (period specified in the `Period` field)
    /// </summary>
    [BsonElement("housingRate"), BsonRepresentation(BsonType.Double)]
    public double HousingRate { get; set; }

    /// <summary>
    /// Period of the cost, e.g. "Academic Year," "Month"
    /// </summary>
    [BsonElement("period"), BsonRepresentation(BsonType.String)]
    public string? Period { get; set; }

    [BsonElement("isSharedBathroom"), BsonRepresentation(BsonType.Boolean)]
    public bool IsSharedBathroom { get; set; }

    [BsonElement("isAirConditioned"), BsonRepresentation(BsonType.Boolean)]
    public bool IsAirConditioned { get; set; }

    [BsonElement("buildingId"), BsonRepresentation(BsonType.ObjectId)]

    public string? BuildingID { get; set; }

}