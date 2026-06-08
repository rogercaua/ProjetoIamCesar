using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documents;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;
    private readonly IOidcExportService _oidcExporter;

    public DocumentsController(
        IDocumentRepository documents,
        IRbacService rbac,
        IAuditService audit,
        IOidcExportService oidcExporter)
    {
        _documents = documents;
        _rbac = rbac;
        _audit = audit;
        _oidcExporter = oidcExporter;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetAll()
    {
        var result = (await _documents.GetAllAsync())
            .Where(document => _rbac.CanViewDocument(User, document))
            .Select(document => document.ToDto(
                _rbac.CanDownloadDocument(User, document),
                _rbac.HasPermission(User, Permissions.DeleteDocuments),
                _rbac.HasPermission(User, Permissions.ExportOidc)))
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequestDto request)
    {
        if (!_rbac.HasPermission(User, Permissions.UploadDocument))
        {
            return Forbid();
        }

        if (request.File is null)
        {
            return BadRequest(new MessageResponseDto { Message = "Arquivo nao enviado." });
        }

        var actor = User.Identity?.Name ?? "unknown";
        var document = await _documents.SaveAsync(request.File, actor, request.Sensitivity);
        await _audit.WriteAsync("document.upload", actor, $"Documento {document.OriginalFileName} criado via API.");

        return CreatedAtAction(nameof(GetAll), document.ToDto(canDownload: true, canExportOidc: true));
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound(new MessageResponseDto { Message = "Documento nao encontrado." });
        }

        if (!_rbac.CanDownloadDocument(User, document))
        {
            return Forbid();
        }

        await _audit.WriteAsync("document.download", User.Identity?.Name ?? "unknown", $"Download via API do documento {document.OriginalFileName}.");
        var stream = await _documents.OpenReadAsync(document);
        return File(stream, document.ContentType, document.OriginalFileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageResponseDto>> Delete(int id)
    {
        if (!_rbac.HasPermission(User, Permissions.DeleteDocuments))
        {
            return Forbid();
        }

        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound(new MessageResponseDto { Message = "Documento nao encontrado." });
        }

        await _documents.DeleteAsync(document);
        await _audit.WriteAsync("document.delete", User.Identity?.Name ?? "unknown", $"Documento {document.OriginalFileName} removido via API.");
        return Ok(new MessageResponseDto { Message = "Documento removido." });
    }

    [HttpPost("{id:int}/export/oidc")]
    public async Task<ActionResult<ExportResultDto>> ExportOidc(int id, OidcExportRequestDto request)
    {
        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound(new MessageResponseDto { Message = "Documento nao encontrado." });
        }

        if (!_rbac.CanViewDocument(User, document) || !_rbac.HasPermission(User, Permissions.ExportOidc))
        {
            return Forbid();
        }

        var actor = User.Identity?.Name ?? "unknown";
        var providerAccount = string.IsNullOrWhiteSpace(request.ProviderAccount)
            ? $"{actor}@gmail.com"
            : request.ProviderAccount;

        var result = await _oidcExporter.ExportAsync(document, actor, providerAccount);
        await _audit.WriteAsync("oidc.export.success", actor, $"Documento {document.OriginalFileName} exportado via API para {providerAccount}.");
        return Ok(result);
    }
}
