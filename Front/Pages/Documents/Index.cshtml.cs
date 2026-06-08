using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Documents;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IDocumentRepository _documents;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;

    public IndexModel(IDocumentRepository documents, IRbacService rbac, IAuditService audit)
    {
        _documents = documents;
        _rbac = rbac;
        _audit = audit;
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    [BindProperty]
    public string Sensitivity { get; set; } = "Interno";

    public IReadOnlyList<DocumentView> Documents { get; private set; } = Array.Empty<DocumentView>();
    public bool CanUpload { get; private set; }
    public string? Message { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (!_rbac.HasPermission(User, Permissions.UploadDocument))
        {
            return Forbid();
        }

        if (Upload is null)
        {
            Message = "Selecione um arquivo para enviar.";
            await LoadAsync();
            return Page();
        }

        var owner = User.Identity?.Name ?? "unknown";
        var document = await _documents.SaveAsync(Upload, owner, Sensitivity);
        await _audit.WriteAsync("document.upload", owner, $"Documento {document.OriginalFileName} criado com classificacao {document.Sensitivity}.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        if (!_rbac.CanDownloadDocument(User, document))
        {
            return Forbid();
        }

        await _audit.WriteAsync("document.download", User.Identity?.Name ?? "unknown", $"Download do documento {document.OriginalFileName}.");
        var stream = await _documents.OpenReadAsync(document);
        return File(stream, document.ContentType, document.OriginalFileName);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!_rbac.HasPermission(User, Permissions.DeleteDocuments))
        {
            return Forbid();
        }

        var document = await _documents.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        await _documents.DeleteAsync(document);
        await _audit.WriteAsync("document.delete", User.Identity?.Name ?? "unknown", $"Documento {document.OriginalFileName} removido.");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        CanUpload = _rbac.HasPermission(User, Permissions.UploadDocument);
        var allDocuments = await _documents.GetAllAsync();
        Documents = allDocuments
            .Where(document => _rbac.CanViewDocument(User, document))
            .Select(document => new DocumentView(
                document,
                _rbac.CanDownloadDocument(User, document),
                _rbac.HasPermission(User, Permissions.DeleteDocuments),
                _rbac.HasPermission(User, Permissions.ExportOidc)))
            .ToList();
    }

    public sealed record DocumentView(
        DocumentRecord Record,
        bool CanDownload,
        bool CanDelete,
        bool CanExportOidc);
}
