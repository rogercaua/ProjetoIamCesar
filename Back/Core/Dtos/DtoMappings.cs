using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Dtos;

public static class DtoMappings
{
    public static DocumentDto ToDto(
        this DocumentRecord document,
        bool canDownload = false,
        bool canDelete = false,
        bool canExportOidc = false) => new()
    {
        Id = document.Id,
        OriginalFileName = document.OriginalFileName,
        ContentType = document.ContentType,
        OwnerUserName = document.OwnerUserName,
        Sensitivity = document.Sensitivity,
        UploadedAt = document.UploadedAt,
        CanDownload = canDownload,
        CanDelete = canDelete,
        CanExportOidc = canExportOidc
    };

    public static RoleDto ToDto(this RoleDefinition role) => new()
    {
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions
    };

    public static UserDto ToDto(this AppUser user) => new()
    {
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Roles = user.Roles
    };

    public static AuditRecordDto ToDto(this AuditRecord record) => new()
    {
        Timestamp = record.Timestamp,
        Action = record.Action,
        Actor = record.Actor,
        Details = record.Details
    };

    public static AuthenticatedUserDto ToAuthenticatedDto(
        this AppUser user,
        IReadOnlyList<string> permissions) => new()
    {
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Roles = user.Roles,
        Permissions = permissions
    };
}
