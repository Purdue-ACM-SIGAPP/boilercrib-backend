namespace SimpleWebAppReact.Entities;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// One student asking another to be roommates
/// </summary>
public class RoommateRequest
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("fromUserId"), BsonRepresentation(BsonType.String)]
    public string? FromUserId { get; set; }

    [BsonElement("toUserId"), BsonRepresentation(BsonType.String)]
    public string? ToUserId { get; set; }

    [BsonElement("message"), BsonRepresentation(BsonType.String)]
    public string? Message { get; set; }

    [BsonElement("status"), BsonRepresentation(BsonType.String)]
    public RoommateRequestStatus Status { get; set; } = RoommateRequestStatus.Pending;

    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }

    /// <summary>when the request was accepted or declined</summary>
    [BsonElement("respondedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// Sent to and from the API as its name, e.g. "Pending"
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RoommateRequestStatus>))]
public enum RoommateRequestStatus
{
    Pending,
    Accepted,
    Declined
}
