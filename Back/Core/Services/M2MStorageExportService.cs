using System.Text.Json;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class M2MStorageExportService : IM2MStorageExportService
{
    private readonly IDocumentRepository _documents;
    private readonly string _exportPath;

    public M2MStorageExportService(IDocumentRepository documents, IWebHostEnvironment environment)
    {
        _documents = documents;
        _exportPath = Path.Combine(environment.ContentRootPath, "Storage", "external", "m2m-storage");
    }

    public async Task<ExportResultDto> ExportAsync(DocumentRecord document, string clientId)
    {
        Directory.CreateDirectory(_exportPath);
        var exportedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{document.OriginalFileName}";
        var exportedFile = Path.Combine(_exportPath, exportedName);

        await using (var source = await _documents.OpenReadAsync(document))
        await using (var destination = File.Create(exportedFile))
        {
            await source.CopyToAsync(destination);
        }

        var artifact = new ExportResultDto
        {
            Protocol = "OAuth2 Client Credentials",
            Actor = clientId,
            Scope = Permissions.ExportM2M,
            DocumentId = document.Id,
            OriginalFileName = document.OriginalFileName,
            ExportedAt = DateTimeOffset.UtcNow,
            StoredAt = exportedFile,
            Metadata = new
            {
                ClientId = clientId,
                TokenType = "Bearer"
            }
        };

        await File.WriteAllTextAsync(
            Path.Combine(_exportPath, $"{Path.GetFileNameWithoutExtension(exportedName)}.m2m.json"),
            JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }));

        return artifact;
    }
}
