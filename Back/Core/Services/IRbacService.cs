using System.Security.Claims;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IRbacService
{
    IReadOnlyList<RoleDefinition> Roles { get; }
    IReadOnlyList<string> GetPermissions(ClaimsPrincipal principal);
    bool HasPermission(ClaimsPrincipal principal, string permission);
    bool CanViewDocument(ClaimsPrincipal principal, DocumentRecord document);
    bool CanDownloadDocument(ClaimsPrincipal principal, DocumentRecord document);
}
