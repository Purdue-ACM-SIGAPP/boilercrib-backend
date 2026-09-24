namespace SimpleWebAppReact.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
/// <summary>
/// Class structure matches 1-1 with User Table in database
/// </summary>
public class User
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("username"), BsonRepresentation(BsonType.String)]
    public string? Username { get; set; }

    /// <summary>
    /// Stored as entered. There is no sign-in check yet: an account is created on submit and
    /// its id is looked up by username (GET api/User?username=...).
    /// (DEPRECATED - REFER TO passwordHashed)
    /// </summary>
    [BsonElement("password"), BsonRepresentation(BsonType.String)]
    public string? Password { get; set; }
    
    [BsonElement("passwordHashed"), BsonRepresentation(BsonType.String)]
    public string? PasswordHashed { get; set; }

    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }

    [BsonElement("phoneNumber"), BsonRepresentation(BsonType.String)]
    public string? PhoneNumber { get; set; }

    [BsonElement("accountType"), BsonRepresentation(BsonType.Int32)]
    public int? AccountType { get; set; }
}
