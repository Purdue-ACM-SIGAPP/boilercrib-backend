namespace SimpleWebAppReact.Entities;
/// <summary>
/// multipart/form-data body for POST api/bugreports.
/// Repeat the "files" and "screenshots" fields once per file.
/// </summary>
public class BugReportSubmission
{
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public string? Description { get; set; }
    public string? Steps { get; set; }

    /// <summary>
    /// required: at least one PNG, JPG or ZIP
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();

    /// <summary>
    /// optional: PNG or JPG only
    /// </summary>
    public List<IFormFile> Screenshots { get; set; } = new();
}

/// <summary>
/// JSON body for PATCH api/bugreports/{id}/status (admins only)
/// </summary>
public class BugReportStatusUpdate
{
    public string? Status { get; set; }
    public string? AdminNotes { get; set; }
}
