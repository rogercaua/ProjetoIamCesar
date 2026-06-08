using System.Security.Claims;
using System.Text.Json;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Exports;

[Authorize]
public sealed class OidcModel : PageModel
{
    private readonly IDocumentRepository _documents;
    private readonly IRbacService _rbac;
    private readonly IOidcExportService _exporter;
    private readonly IAuditService _audit;

    public OidcModel(IDocumentRepository documents, IRbacService rbac, IOidcExportService exporter, IAuditService audit)
    {
        _documents = documents;
        _rbac = rbac;
        _exporter = exporter;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)]
    public int DocumentId { get; set; }

    [BindProperty]
    public string ProviderAccount { get; set; } = "";

    public DocumentRecord? Document { get; private set; }
    public string? ExportResult { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAndAuthorizeAsync())
        {
            return Forbid();
        }

        ProviderAccount = User.FindFirstValue(ClaimTypes.Email)?.Replace("@iam.local", "@gmail.com")
            ?? $"{User.Identity?.Name}@gmail.com";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await LoadAndAuthorizeAsync())
        {
            return Forbid();
        }

        var actor = User.Identity?.Name ?? "unknown";
        var result = await _exporter.ExportAsync(Document!, actor, ProviderAccount);
        await _audit.WriteAsync("oidc.export.success", actor, $"Documento {Document!.OriginalFileName} exportado para {ProviderAccount}.");
        ExportResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return Page();
    }

    private async Task<bool> LoadAndAuthorizeAsync()
    {
        Document = await _documents.FindAsync(DocumentId);
        return Document is not null
            && _rbac.CanViewDocument(User, Document)
            && _rbac.HasPermission(User, Permissions.ExportOidc);
    }
}
