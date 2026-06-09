using System.Security.Claims;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class RbacService : IRbacService
{
    private readonly IReadOnlyDictionary<string, RoleDefinition> _roles;

    public RbacService()
    {
        _roles = new Dictionary<string, RoleDefinition>
        {
            [AppRoles.Admin] = new RoleDefinition
            {
                Name = AppRoles.Admin,
                Description = "Administra usuarios, documentos, auditoria e exportacoes.",
                Permissions = new[]
                {
                    Permissions.UploadDocument,
                    Permissions.ViewOwnDocuments,
                    Permissions.ViewAllDocuments,
                    Permissions.DownloadOwnDocuments,
                    Permissions.DownloadAllDocuments,
                    Permissions.DeleteDocuments,
                    Permissions.ExportGoogleDrive,
                    Permissions.ExportM2M,
                    Permissions.ManageRoles,
                    Permissions.ViewAudit
                }
            },
            [AppRoles.Manager] = new RoleDefinition
            {
                Name = AppRoles.Manager,
                Description = "Gerencia documentos e exportacoes.",
                Permissions = new[]
                {
                    Permissions.UploadDocument,
                    Permissions.ViewAllDocuments,
                    Permissions.DownloadAllDocuments,
                    Permissions.ExportGoogleDrive
                }
            },
            [AppRoles.User] = new RoleDefinition
            {
                Name = AppRoles.User,
                Description = "Envia, visualiza e baixa apenas seus proprios documentos.",
                Permissions = new[]
                {
                    Permissions.UploadDocument,
                    Permissions.ViewOwnDocuments,
                    Permissions.DownloadOwnDocuments,
                    Permissions.ExportGoogleDrive
                }
            },
            [AppRoles.Auditor] = new RoleDefinition
            {
                Name = AppRoles.Auditor,
                Description = "Consulta apenas a trilha de auditoria sem alterar dados.",
                Permissions = new[]
                {
                    Permissions.ViewAudit
                }
            },
            [AppRoles.Service] = new RoleDefinition
            {
                Name = AppRoles.Service,
                Description = "Conta tecnica para exportacao M2M por OAuth2.",
                Permissions = new[]
                {
                    Permissions.ExportM2M
                }
            }
        };
    }

    public IReadOnlyList<RoleDefinition> Roles => _roles.Values.OrderBy(role => role.Name).ToList();

    public IReadOnlyList<string> GetPermissions(ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => _roles.ContainsKey(role))
            .SelectMany(role => _roles[role].Permissions)
            .Distinct()
            .OrderBy(permission => permission)
            .ToList();
    }

    public bool HasPermission(ClaimsPrincipal principal, string permission)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Any(role => _roles.TryGetValue(role, out var definition)
                && definition.Permissions.Contains(permission));
    }

    public bool CanViewDocument(ClaimsPrincipal principal, DocumentRecord document)
    {
        if (HasPermission(principal, Permissions.ViewAllDocuments))
        {
            return true;
        }

        return HasPermission(principal, Permissions.ViewOwnDocuments)
            && string.Equals(principal.Identity?.Name, document.OwnerUserName, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanDownloadDocument(ClaimsPrincipal principal, DocumentRecord document)
    {
        if (HasPermission(principal, Permissions.DownloadAllDocuments))
        {
            return true;
        }

        return HasPermission(principal, Permissions.DownloadOwnDocuments)
            && string.Equals(principal.Identity?.Name, document.OwnerUserName, StringComparison.OrdinalIgnoreCase);
    }
}
