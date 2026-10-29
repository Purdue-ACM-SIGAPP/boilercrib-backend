namespace SimpleWebAppReact.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// A user's roommate-matching bio: who they are (name, gender, age, year, major), their interests,
/// housing preferences, contact info, and a free-form description, so other users can find them (issue #87)
/// </summary>
public class RoommateBio
{
    // static fields storing definitions of max/min age values
    public const int MIN_AGE = 16;
    public const int MAX_AGE = 100;

    // database elements
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public string? UserId { get; set; }

    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }

    [BsonElement("gender"), BsonRepresentation(BsonType.String)]
    public string? Gender { get; set; }

    [BsonElement("age"), BsonRepresentation(BsonType.Int32)]
    public int? Age { get; set; }

    /// <summary>
    /// a ClassYear value
    /// </summary>
    [BsonElement("year"), BsonRepresentation(BsonType.Int32)]
    public int? Year { get; set; }

    [BsonElement("major"), BsonRepresentation(BsonType.String)]
    public string? Major { get; set; }

    [BsonElement("bio"), BsonRepresentation(BsonType.String)]
    public string? Bio { get; set; }

    /// <summary>
    /// hobbies and other interests
    /// </summary>
    [BsonElement("interests")]
    public List<string> Interests { get; set; } = new();

    /// <summary>
    /// ids of buildings (see Entities/Building.cs) this user is interested in living in
    /// </summary>
    [BsonElement("preferredBuildingIds")]
    public List<string> PreferredBuildingIds { get; set; } = new();

    [BsonElement("contactInfo"), BsonRepresentation(BsonType.String)]
    public string? ContactInfo { get; set; }

    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
}
