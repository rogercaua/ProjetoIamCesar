using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class GoogleDriveExportService : IGoogleDriveExportService
{
    private readonly IDocumentRepository _documents;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly GoogleDriveOptions _options;

    public GoogleDriveExportService(
        IDocumentRepository documents,
        IHttpContextAccessor httpContextAccessor,
        IOptions<GoogleDriveOptions> options)
    {
        _documents = documents;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public async Task<ExportResultDto> ExportAsync(DocumentRecord document, string actor, string? driveFileName)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Contexto HTTP indisponivel.");

        var googleAuth = await httpContext.AuthenticateAsync("GoogleExternal");
        var accessToken = googleAuth.Properties?.GetTokenValue("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Conecte uma conta Google antes de exportar para o Drive.");
        }

        var credential = GoogleCredential.FromAccessToken(accessToken);
        using var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _options.ApplicationName
        });

        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = string.IsNullOrWhiteSpace(driveFileName) ? document.OriginalFileName : driveFileName
        };

        if (!string.IsNullOrWhiteSpace(_options.UploadFolderId))
        {
            metadata.Parents = new[] { _options.UploadFolderId };
        }

        await using var source = await _documents.OpenReadAsync(document);
        var upload = drive.Files.Create(metadata, source, document.ContentType);
        upload.Fields = "id,name,webViewLink";

        var progress = await upload.UploadAsync();
        if (progress.Status == UploadStatus.Failed)
        {
            throw new InvalidOperationException(progress.Exception?.Message ?? "Falha ao enviar arquivo para o Google Drive.");
        }

        var uploaded = upload.ResponseBody
            ?? throw new InvalidOperationException("Google Drive nao retornou metadados do arquivo enviado.");

        return new ExportResultDto
        {
            Protocol = "OpenID Connect + Google Drive API",
            Actor = actor,
            Scope = "openid profile email https://www.googleapis.com/auth/drive.file",
            DocumentId = document.Id,
            OriginalFileName = document.OriginalFileName,
            ExportedAt = DateTimeOffset.UtcNow,
            StoredAt = uploaded.WebViewLink ?? uploaded.Id ?? "",
            Metadata = new
            {
                DriveFileId = uploaded.Id,
                DriveFileName = uploaded.Name,
                WebViewLink = uploaded.WebViewLink
            }
        };
    }
}
