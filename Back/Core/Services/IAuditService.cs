using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IAuditService
{
    Task WriteAsync(string action, string actor, string details);
    Task<IReadOnlyList<AuditRecord>> GetRecentAsync(int limit = 80);
}
