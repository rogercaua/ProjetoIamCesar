namespace DocumentPortalIam.Back.Core.Models;

public sealed class AuditRecord
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Details { get; set; } = "";
}
