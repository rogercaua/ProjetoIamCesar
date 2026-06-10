using System.Security.Claims;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IRbacService
{
    IReadOnlyList<RoleDefinition> Roles { get; }
    IReadOnlyList<string> GetPermissions(ClaimsPrincipal principal);
    IReadOnlyList<string> GetAllowedUploadSensitivities(ClaimsPrincipal principal);
    bool HasPermission(ClaimsPrincipal principal, string permission);
    bool CanAccessDocuments(ClaimsPrincipal principal);
    bool CanUploadSensitivity(ClaimsPrincipal principal, string sensitivity);
    bool CanViewDocument(ClaimsPrincipal principal, DocumentRecord document);
    bool CanDownloadDocument(ClaimsPrincipal principal, DocumentRecord document);
}
