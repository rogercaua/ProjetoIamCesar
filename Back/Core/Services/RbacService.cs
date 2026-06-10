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
                    Permissions.ViewAllDocuments,
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
                Description = "Envia, visualiza e baixa apenas documentos publicos.",
                Permissions = new[]
                {
                    Permissions.UploadDocument,
                    Permissions.ViewPublicDocuments,
                    Permissions.DownloadPublicDocuments,
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

    public IReadOnlyList<string> GetAllowedUploadSensitivities(ClaimsPrincipal principal)
    {
        if (!HasPermission(principal, Permissions.UploadDocument))
        {
            return Array.Empty<string>();
        }

        if (principal.IsInRole(AppRoles.Admin) || principal.IsInRole(AppRoles.Manager))
        {
            return DocumentSensitivities.All;
        }

        return new[] { DocumentSensitivities.Public };
    }

    public bool HasPermission(ClaimsPrincipal principal, string permission)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Any(role => _roles.TryGetValue(role, out var definition)
                && definition.Permissions.Contains(permission));
    }

    public bool CanAccessDocuments(ClaimsPrincipal principal)
    {
        return HasPermission(principal, Permissions.ViewAllDocuments)
            || HasPermission(principal, Permissions.ViewPublicDocuments);
    }

    public bool CanUploadSensitivity(ClaimsPrincipal principal, string sensitivity)
    {
        var normalized = DocumentSensitivities.Normalize(sensitivity);
        return !string.IsNullOrWhiteSpace(normalized)
            && GetAllowedUploadSensitivities(principal)
                .Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public bool CanViewDocument(ClaimsPrincipal principal, DocumentRecord document)
    {
        if (HasPermission(principal, Permissions.ViewAllDocuments))
        {
            return true;
        }

        if (HasPermission(principal, Permissions.ViewPublicDocuments)
            && IsPublic(document))
        {
            return true;
        }

        return false;
    }

    public bool CanDownloadDocument(ClaimsPrincipal principal, DocumentRecord document)
    {
        if (HasPermission(principal, Permissions.DownloadAllDocuments))
        {
            return true;
        }

        if (HasPermission(principal, Permissions.DownloadPublicDocuments)
            && IsPublic(document))
        {
            return true;
        }

        return false;
    }

    private static bool IsPublic(DocumentRecord document)
    {
        return string.Equals(
            DocumentSensitivities.Normalize(document.Sensitivity),
            DocumentSensitivities.Public,
            StringComparison.OrdinalIgnoreCase);
    }
}
