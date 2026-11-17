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

    /// <summary>
    /// how to reach this user. Only the user, admins, and students they accepted a roommate request
    /// with can see it; everyone else gets null.
    /// </summary>
    [BsonElement("contactInfo"), BsonRepresentation(BsonType.String)]
    public string? ContactInfo { get; set; }

    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    // matching fields, used to rank compatibility (see Services/RoommateMatcher.cs). All optional:
    // a blank answer counts as neutral. Lifestyle answers use a 1-5 scale.
    public const int MIN_SCALE = 1;
    public const int MAX_SCALE = 5;

    /// <summary>
    /// when the user wants to move in, e.g. "Fall 2027"
    /// </summary>
    [BsonElement("term"), BsonRepresentation(BsonType.String)]
    public string? Term { get; set; }

    /// <summary>
    /// housing budget in dollars per semester
    /// </summary>
    [BsonElement("budgetMin"), BsonRepresentation(BsonType.Int32)]
    public int? BudgetMin { get; set; }

    [BsonElement("budgetMax"), BsonRepresentation(BsonType.Int32)]
    public int? BudgetMax { get; set; }

    /// <summary>
    /// 1 = early bird, 5 = night owl
    /// </summary>
    [BsonElement("sleepSchedule"), BsonRepresentation(BsonType.Int32)]
    public int? SleepSchedule { get; set; }

    /// <summary>
    /// 1 = relaxed, 5 = very tidy
    /// </summary>
    [BsonElement("cleanliness"), BsonRepresentation(BsonType.Int32)]
    public int? Cleanliness { get; set; }

    /// <summary>
    /// 1 = needs quiet, 5 = fine with noise
    /// </summary>
    [BsonElement("noiseTolerance"), BsonRepresentation(BsonType.Int32)]
    public int? NoiseTolerance { get; set; }

    /// <summary>
    /// 1 = rarely has guests, 5 = guests often
    /// </summary>
    [BsonElement("guestFrequency"), BsonRepresentation(BsonType.Int32)]
    public int? GuestFrequency { get; set; }

    [BsonElement("smoker"), BsonRepresentation(BsonType.Boolean)]
    public bool? Smoker { get; set; }

    /// <summary>
    /// false hides the bio from browsing and matches. Bios saved before this field existed count as looking.
    /// </summary>
    [BsonElement("isLooking"), BsonRepresentation(BsonType.Boolean)]
    public bool IsLooking { get; set; } = true;
}
