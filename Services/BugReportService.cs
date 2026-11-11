using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

/// <summary>
/// Everything behind the bug report form: validation, file storage, reference numbers and queries.
/// Reports are in the bugReport collection; their files are in the bugReportFiles GridFS bucket.
/// </summary>
public class BugReportService
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const long MaxRequestBytes = 40 * 1024 * 1024;
    public const int MaxAttachments = 5;
    public const int MaxScreenshots = 3;
    public const int MaxTextLength = 5000;
    private const int MaxFileNameLength = 100;

    private const string PngType = "image/png";
    private const string JpegType = "image/jpeg";
    private const string ZipType = "application/zip";
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] EmptyZipMagic = { 0x50, 0x4B, 0x05, 0x06 };

    private readonly IMongoCollection<BugReport> _reports;
    private readonly IMongoCollection<BsonDocument> _counters;
    private readonly GridFSBucket _files;

    public BugReportService(MongoDbService mongoDbService)
    {
        var db = mongoDbService.Database;
        _reports = db.GetCollection<BugReport>("bugReport");
        _counters = db.GetCollection<BsonDocument>("counters");
        _files = new GridFSBucket(db, new GridFSBucketOptions { BucketName = "bugReportFiles" });

        _reports.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<BugReport>(Builders<BugReport>.IndexKeys.Ascending(r => r.Reference),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<BugReport>(Builders<BugReport>.IndexKeys.Ascending(r => r.UserId).Descending(r => r.CreatedAt)),
            new CreateIndexModel<BugReport>(Builders<BugReport>.IndexKeys.Ascending(r => r.Status).Descending(r => r.CreatedAt)),
        });
    }

    /// <summary>
    /// validates and stores a submission with its files; returns the saved report or an error message
    /// </summary>
    public async Task<(BugReport? Report, string? Error)> CreateAsync(BugReportSubmission submission, string? userId)
    {
        var error = Validate(submission);
        if (error != null)
        {
            return (null, error);
        }

        // Check every file before storing any, so a bad file doesn't leave orphans behind
        var uploads = new List<(IFormFile File, string Kind, string ContentType)>();
        var incoming = submission.Files.Select(f => (File: f, Kind: BugReportAttachment.KindAttachment))
            .Concat(submission.Screenshots.Select(f => (File: f, Kind: BugReportAttachment.KindScreenshot)));
        foreach (var (file, kind) in incoming)
        {
            var name = CleanFileName(file.FileName);
            if (file.Length == 0)
            {
                return (null, $"{name} is empty.");
            }

            if (file.Length > MaxFileBytes)
            {
                return (null, $"{name} is over 10 MB.");
            }

            var contentType = await SniffContentTypeAsync(file);
            if (kind == BugReportAttachment.KindScreenshot && contentType is not (PngType or JpegType))
            {
                return (null, $"{name}: screenshots must be PNG or JPG.");
            }

            if (contentType is null)
            {
                return (null, $"{name}: attachments must be PNG, JPG, or ZIP.");
            }

            uploads.Add((file, kind, contentType));
        }

        var now = DateTime.UtcNow;
        var report = new BugReport
        {
            Reference = await NextReferenceAsync(),
            UserId = userId,
            Category = submission.Category,
            Severity = submission.Severity,
            Description = submission.Description!.Trim(),
            Steps = string.IsNullOrWhiteSpace(submission.Steps) ? null : submission.Steps.Trim(),
            Status = BugReport.DefaultStatus,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            foreach (var (file, kind, contentType) in uploads)
            {
                var fileName = CleanFileName(file.FileName);
                await using var stream = file.OpenReadStream();
                var fileId = await _files.UploadFromStreamAsync(fileName, stream, new GridFSUploadOptions
                {
                    Metadata = new BsonDocument { { "contentType", contentType }, { "reference", report.Reference } },
                });
                report.Attachments.Add(new BugReportAttachment
                {
                    FileId = fileId.ToString(),
                    FileName = fileName,
                    ContentType = contentType,
                    Size = file.Length,
                    Kind = kind,
                });
            }

            await _reports.InsertOneAsync(report);
        }
        catch
        {
            await DeleteFilesAsync(report.Attachments);
            throw;
        }

        return (report, null);
    }

    /// <summary>
    /// a user's reports, newest first
    /// </summary>
    public Task<List<BugReport>> GetByUserAsync(string? userId) =>
        _reports.Find(r => r.UserId == userId).SortByDescending(r => r.CreatedAt).ToListAsync();

    /// <summary>
    /// all reports matching the optional filters, newest first
    /// </summary>
    public Task<List<BugReport>> GetAsync(string? status, string? severity, string? category)
    {
        var f = Builders<BugReport>.Filter;
        var filter = f.Empty;
        if (!string.IsNullOrEmpty(status)) filter &= f.Eq(r => r.Status, status);
        if (!string.IsNullOrEmpty(severity)) filter &= f.Eq(r => r.Severity, severity);
        if (!string.IsNullOrEmpty(category)) filter &= f.Eq(r => r.Category, category);

        return _reports.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<BugReport?> FindAsync(string? id) =>
        MongoDbService.IsValidId(id) ? await _reports.Find(r => r.Id == id).FirstOrDefaultAsync() : null;

    /// <summary>
    /// opens a stored file for download, or null if it's missing
    /// </summary>
    public async Task<Stream?> OpenAttachmentAsync(BugReportAttachment attachment)
    {
        if (!MongoDbService.IsValidId(attachment.FileId))
        {
            return null;
        }

        try
        {
            return await _files.OpenDownloadStreamAsync(ObjectId.Parse(attachment.FileId));
        }
        catch (GridFSFileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// sets a report's status and, optionally, admin notes; Report is null with no Error when the id doesn't exist
    /// </summary>
    public async Task<(BugReport? Report, string? Error)> UpdateStatusAsync(string id, BugReportStatusUpdate update)
    {
        if (!BugReport.Statuses.Contains(update.Status))
        {
            return (null, $"Status must be one of: {string.Join(", ", BugReport.Statuses)}.");
        }

        if (update.AdminNotes?.Length > MaxTextLength)
        {
            return (null, $"Admin notes must be at most {MaxTextLength} characters.");
        }

        if (!MongoDbService.IsValidId(id))
        {
            return (null, null);
        }

        var changes = Builders<BugReport>.Update
            .Set(r => r.Status, update.Status)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        if (update.AdminNotes != null)
        {
            changes = changes.Set(r => r.AdminNotes, update.AdminNotes.Trim());
        }

        var updated = await _reports.FindOneAndUpdateAsync(r => r.Id == id, changes,
            new FindOneAndUpdateOptions<BugReport> { ReturnDocument = ReturnDocument.After });
        return (updated, null);
    }

    /// <summary>
    /// deletes a report and its files; false if it doesn't exist
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var report = await FindAsync(id);
        if (report is null)
        {
            return false;
        }

        await _reports.DeleteOneAsync(r => r.Id == id);
        await DeleteFilesAsync(report.Attachments);
        return true;
    }

    private static string? Validate(BugReportSubmission s)
    {
        if (!BugReport.Categories.Contains(s.Category))
        {
            return $"Category must be one of: {string.Join(", ", BugReport.Categories)}.";
        }

        if (!BugReport.Severities.Contains(s.Severity))
        {
            return $"Severity must be one of: {string.Join(", ", BugReport.Severities)}.";
        }

        if (string.IsNullOrWhiteSpace(s.Description))
        {
            return "Describe the issue to continue.";
        }

        if (s.Description.Length > MaxTextLength || s.Steps?.Length > MaxTextLength)
        {
            return $"Description and reproduction steps must each be at most {MaxTextLength} characters.";
        }

        if (s.Files.Count == 0)
        {
            return "Add at least one attachment to submit this report.";
        }

        if (s.Files.Count > MaxAttachments)
        {
            return $"Add at most {MaxAttachments} attachments.";
        }

        if (s.Screenshots.Count > MaxScreenshots)
        {
            return $"Add at most {MaxScreenshots} screenshots.";
        }

        return null;
    }

    /// <summary>
    /// the file's type from its first bytes; the client's Content-Type is not trusted
    /// </summary>
    private static async Task<string?> SniffContentTypeAsync(IFormFile file)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        return ContentTypeOf(header.AsSpan(0, read));
    }

    private static string? ContentTypeOf(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PngMagic)) return PngType;
        if (header.StartsWith(JpegMagic)) return JpegType;
        if (header.StartsWith(ZipMagic) || header.StartsWith(EmptyZipMagic)) return ZipType;
        return null;
    }

    private static string CleanFileName(string? name)
    {
        var baseName = Path.GetFileName((name ?? string.Empty).Replace('\\', '/'));
        var clean = new string(baseName.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (clean.Length == 0)
        {
            return "attachment";
        }

        return clean.Length > MaxFileNameLength ? clean[^MaxFileNameLength..] : clean;
    }

    /// <summary>
    /// next reference from an atomic counter: BR-0001, BR-0002, ...
    /// </summary>
    private async Task<string> NextReferenceAsync()
    {
        var counter = await _counters.FindOneAndUpdateAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "bugReport"),
            Builders<BsonDocument>.Update.Inc("seq", 1),
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
        return $"BR-{counter["seq"].ToInt64():D4}";
    }

    private async Task DeleteFilesAsync(IEnumerable<BugReportAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            if (!MongoDbService.IsValidId(attachment.FileId))
            {
                continue;
            }

            try
            {
                await _files.DeleteAsync(ObjectId.Parse(attachment.FileId));
            }
            catch (GridFSFileNotFoundException)
            {
                // already gone
            }
        }
    }
}
