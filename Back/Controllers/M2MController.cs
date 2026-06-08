using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Route("api/m2m")]
public sealed class M2MController : ControllerBase
{
    private readonly IM2MTokenService _tokens;
    private readonly IDocumentRepository _documents;
    private readonly IM2MStorageExportService _exporter;
    private readonly IAuditService _audit;

    public M2MController(
        IM2MTokenService tokens,
        IDocumentRepository documents,
        IM2MStorageExportService exporter,
        IAuditService audit)
    {
        _tokens = tokens;
        _documents = documents;
        _exporter = exporter;
        _audit = audit;
    }

    [HttpPost("export/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ExportResultDto>> Export(int id)
    {
        var authorization = Request.Headers.Authorization.ToString();
        var accessToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : "";

        if (!_tokens.Validate(accessToken, Permissions.ExportM2M, out var clientId))
        {
            await _audit.WriteAsync("m2m.export.denied", "m2m-client", $"Documento {id}: token ausente ou invalido.");
            return Unauthorized(new MessageResponseDto { Message = "Token M2M ausente ou invalido." });
        }

        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound(new MessageResponseDto { Message = "Documento nao encontrado." });
        }

        var result = await _exporter.ExportAsync(document, clientId);
        await _audit.WriteAsync("m2m.export.success", clientId, $"Documento {document.OriginalFileName} exportado para storage externo.");
        return Ok(result);
    }
}
