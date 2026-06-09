namespace DocumentPortalIam.Back.Core.Models;

public sealed class DocumentRecord
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeInBytes { get; set; }
    public string OwnerUserName { get; set; } = "";
    public string Sensitivity { get; set; } = "Interno";
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
