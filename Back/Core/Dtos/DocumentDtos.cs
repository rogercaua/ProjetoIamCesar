namespace DocumentPortalIam.Back.Core.Dtos;

public sealed class DocumentDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeInBytes { get; set; }
    public string OwnerUserName { get; set; } = "";
    public string Sensitivity { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
    public bool CanDownload { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExportGoogleDrive { get; set; }
}

public sealed class UploadDocumentRequestDto
{
    public IFormFile? File { get; set; }
    public string Sensitivity { get; set; } = "Interno";
}

public sealed class GoogleDriveExportRequestDto
{
    public string DriveFileName { get; set; } = "";
}

public sealed class ExportResultDto
{
    public string Protocol { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Scope { get; set; } = "";
    public int DocumentId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public DateTimeOffset ExportedAt { get; set; }
    public string StoredAt { get; set; } = "";
    public object? Metadata { get; set; }
}
