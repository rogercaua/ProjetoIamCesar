namespace DocumentPortalIam.Back.Core.Models;

public static class AppRoles
{
    public const string Admin = "Administrador";
    public const string Manager = "Gestor";
    public const string User = "Usuario";
    public const string Auditor = "Auditor";
    public const string Service = "ServicoM2M";
}

public static class Permissions
{
    public const string UploadDocument = "documents.upload";
    public const string ViewOwnDocuments = "documents.view.own";
    public const string ViewAllDocuments = "documents.view.all";
    public const string DownloadOwnDocuments = "documents.download.own";
    public const string DownloadAllDocuments = "documents.download.all";
    public const string DeleteDocuments = "documents.delete";
    public const string ExportGoogleDrive = "exports.google_drive";
    public const string ExportM2M = "exports.m2m";
    public const string ManageRoles = "users.manage.roles";
    public const string ViewAudit = "audit.view";
}

public sealed class RoleDefinition
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
