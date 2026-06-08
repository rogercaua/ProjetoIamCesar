using System.Text.Json;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class OidcExportService : IOidcExportService
{
    private readonly IDocumentRepository _documents;
    private readonly string _exportPath;

    public OidcExportService(IDocumentRepository documents, IWebHostEnvironment environment)
    {
        _documents = documents;
        _exportPath = Path.Combine(environment.ContentRootPath, "Storage", "external", "oidc-google-drive");
    }

    public async Task<ExportResultDto> ExportAsync(DocumentRecord document, string userName, string providerAccount)
    {
        Directory.CreateDirectory(_exportPath);
        var exportedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{document.OriginalFileName}";
        var exportedFile = Path.Combine(_exportPath, exportedName);

        await using (var source = await _documents.OpenReadAsync(document))
        await using (var destination = File.Create(exportedFile))
        {
            await source.CopyToAsync(destination);
        }

        var claims = new
        {
            Provider = "Google Drive demo",
            Subject = providerAccount,
            RequestedBy = userName,
            IdTokenClaims = new
            {
                iss = "https://accounts.google.com",
                aud = "document-portal-iam-demo",
                sub = providerAccount,
                email = providerAccount,
                name = providerAccount.Split('@')[0]
            },
            StoredAt = exportedFile
        };

        var artifact = new ExportResultDto
        {
            Protocol = "OpenID Connect",
            Actor = userName,
            Scope = "openid email profile",
            DocumentId = document.Id,
            OriginalFileName = document.OriginalFileName,
            ExportedAt = DateTimeOffset.UtcNow,
            StoredAt = exportedFile,
            Metadata = claims
        };

        await File.WriteAllTextAsync(
            Path.Combine(_exportPath, $"{Path.GetFileNameWithoutExtension(exportedName)}.oidc.json"),
            JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }));

        return artifact;
    }
}
