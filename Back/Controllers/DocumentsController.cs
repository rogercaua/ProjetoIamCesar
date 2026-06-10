using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documents;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;
    private readonly IGoogleDriveExportService _googleDriveExporter;

    public DocumentsController(
        IDocumentRepository documents,
        IRbacService rbac,
        IAuditService audit,
        IGoogleDriveExportService googleDriveExporter)
    {
        _documents = documents;
        _rbac = rbac;
        _audit = audit;
        _googleDriveExporter = googleDriveExporter;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista documentos permitidos pelo RBAC.",
        Description = "Usuario logado. Administrador e Gestor visualizam todos; Usuario comum visualiza apenas documentos Publico; Auditor nao acessa documentos.")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetAll()
    {
        if (!_rbac.CanAccessDocuments(User))
        {
            return Forbid();
        }

        var result = (await _documents.GetAllAsync())
            .Where(document => _rbac.CanViewDocument(User, document))
            .Select(document => document.ToDto(
                _rbac.CanDownloadDocument(User, document),
                _rbac.HasPermission(User, Permissions.DeleteDocuments),
                _rbac.HasPermission(User, Permissions.ExportGoogleDrive)))
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Envia documento.",
        Description = "Administrador e Gestor podem enviar Publico, Interno ou Confidencial. Usuario comum pode enviar apenas Publico. Auditor nao envia documentos.")]
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

        var normalizedSensitivity = DocumentSensitivities.Normalize(request.Sensitivity);
        if (string.IsNullOrWhiteSpace(normalizedSensitivity))
        {
            return BadRequest(new MessageResponseDto { Message = "Classificacao invalida." });
        }

        if (!_rbac.CanUploadSensitivity(User, normalizedSensitivity))
        {
            return Forbid();
        }

        var actor = User.Identity?.Name ?? "unknown";
        DocumentRecord document;
        try
        {
            document = await _documents.SaveAsync(request.File, actor, normalizedSensitivity);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new MessageResponseDto { Message = exception.Message });
        }

        await _audit.WriteAsync("document.upload", actor, $"Documento {document.OriginalFileName} criado via API.");

        if (IsHtmlFormRequest())
        {
            return Redirect("/dashboard");
        }

        return CreatedAtAction(nameof(GetAll), document.ToDto(canDownload: true, canExportGoogleDrive: true));
    }

    [HttpGet("download")]
    [SwaggerOperation(
        Summary = "Baixa documento por formulario HTML.",
        Description = "Usuario logado. Recebe o ID por query string para permitir front estatico sem JavaScript.")]
    public Task<IActionResult> DownloadFromQuery([FromQuery] int id)
    {
        return Download(id);
    }

    [HttpGet("{id:int}/download")]
    [SwaggerOperation(
        Summary = "Baixa documento.",
        Description = "Administrador e Gestor baixam qualquer documento. Usuario comum baixa apenas documentos Publico. Auditor nao possui permissao de download.")]
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
    [SwaggerOperation(
        Summary = "Remove documento.",
        Description = "Apenas Administrador. Remove metadados do SQLite, apaga o arquivo fisico em Storage/Documents e registra auditoria.")]
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

    [HttpPost("delete")]
    [SwaggerOperation(
        Summary = "Remove documento por formulario HTML.",
        Description = "Apenas Administrador. Rota auxiliar em POST para front estatico sem JavaScript.")]
    public async Task<IActionResult> DeleteFromForm([FromForm] int id)
    {
        var result = await Delete(id);
        if (result.Result is OkObjectResult)
        {
            return Redirect("/dashboard");
        }

        return result.Result ?? Ok(result.Value);
    }

    [HttpPost("{id:int}/export/google-drive")]
    [SwaggerOperation(
        Summary = "Exporta documento para o Google Drive.",
        Description = "Administrador, Gestor ou Usuario com acesso ao documento. Usuario comum exporta apenas documentos Publico. Requer conta Google conectada via OpenID Connect e usa Google Drive API.")]
    public async Task<ActionResult<ExportResultDto>> ExportGoogleDrive(int id, GoogleDriveExportRequestDto request)
    {
        return await ExportGoogleDriveCore(id, request);
    }

    [HttpPost("export/google-drive")]
    [SwaggerOperation(
        Summary = "Exporta para Google Drive por formulario HTML.",
        Description = "Rota auxiliar em POST para front estatico sem JavaScript. Recebe ID do documento no corpo do formulario.")]
    public async Task<IActionResult> ExportGoogleDriveFromForm([FromForm] int id, [FromForm] string driveFileName = "")
    {
        var result = await ExportGoogleDriveCore(id, new GoogleDriveExportRequestDto
        {
            DriveFileName = driveFileName
        });

        if (result.Result is OkObjectResult)
        {
            return Redirect("/dashboard");
        }

        return result.Result ?? Ok(result.Value);
    }

    private async Task<ActionResult<ExportResultDto>> ExportGoogleDriveCore(int id, GoogleDriveExportRequestDto request)
    {
        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound(new MessageResponseDto { Message = "Documento nao encontrado." });
        }

        if (!_rbac.CanViewDocument(User, document) || !_rbac.HasPermission(User, Permissions.ExportGoogleDrive))
        {
            return Forbid();
        }

        var actor = User.Identity?.Name ?? "unknown";
        ExportResultDto result;
        try
        {
            result = await _googleDriveExporter.ExportAsync(document, actor, request.DriveFileName);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new MessageResponseDto { Message = exception.Message });
        }

        await _audit.WriteAsync("google.drive.export.success", actor, $"Documento {document.OriginalFileName} exportado para Google Drive.");
        return Ok(result);
    }

    private bool IsHtmlFormRequest()
    {
        return Request.HasFormContentType
            || Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
