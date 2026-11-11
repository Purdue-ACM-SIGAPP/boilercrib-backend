namespace SimpleWebAppReact.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
/// <summary>
/// Class structure matches 1-1 with BugReport Table in database.
/// Files live in the bugReportFiles GridFS bucket; Attachments holds their metadata only.
/// </summary>
public class BugReport
{
    // Must match CATEGORIES and SEVERITIES in the frontend's BugReportForm.js
    public static readonly string[] Categories = { "Crash / freeze", "UI glitch", "Performance", "Data loss", "Other" };
    public static readonly string[] Severities = { "Low", "Medium", "High", "Critical" };
    public static readonly string[] Statuses = { "Submitted", "In review", "Resolved", "Closed" };
    public const string DefaultStatus = "Submitted";

    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// human-friendly reference shown to the reporter, e.g. BR-0042
    /// </summary>
    [BsonElement("reference"), BsonRepresentation(BsonType.String)]
    public string? Reference { get; set; }

    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public string? UserId { get; set; }

    [BsonElement("category"), BsonRepresentation(BsonType.String)]
    public string? Category { get; set; }

    [BsonElement("severity"), BsonRepresentation(BsonType.String)]
    public string? Severity { get; set; }

    [BsonElement("description"), BsonRepresentation(BsonType.String)]
    public string? Description { get; set; }

    [BsonElement("steps"), BsonRepresentation(BsonType.String)]
    public string? Steps { get; set; }

    [BsonElement("status"), BsonRepresentation(BsonType.String)]
    public string? Status { get; set; }

    [BsonElement("adminNotes"), BsonRepresentation(BsonType.String)]
    public string? AdminNotes { get; set; }

    [BsonElement("attachments")]
    public List<BugReportAttachment> Attachments { get; set; } = new();

    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }

    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// metadata for one uploaded file; the bytes are in GridFS under FileId
/// </summary>
public class BugReportAttachment
{
    public const string KindAttachment = "attachment";
    public const string KindScreenshot = "screenshot";

    [BsonElement("fileId"), BsonRepresentation(BsonType.ObjectId)]
    public string? FileId { get; set; }

    [BsonElement("fileName"), BsonRepresentation(BsonType.String)]
    public string? FileName { get; set; }

    [BsonElement("contentType"), BsonRepresentation(BsonType.String)]
    public string? ContentType { get; set; }

    [BsonElement("size"), BsonRepresentation(BsonType.Int64)]
    public long Size { get; set; }

    /// <summary>
    /// "attachment" (required upload) or "screenshot" (optional slot)
    /// </summary>
    [BsonElement("kind"), BsonRepresentation(BsonType.String)]
    public string? Kind { get; set; }
}
