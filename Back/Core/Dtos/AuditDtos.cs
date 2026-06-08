namespace DocumentPortalIam.Back.Core.Dtos;

public sealed class AuditRecordDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Details { get; set; } = "";
}
