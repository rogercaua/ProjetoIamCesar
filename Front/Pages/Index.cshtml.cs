using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages;

public class IndexModel : PageModel
{
    private readonly IDocumentRepository _documents;
    private readonly IRbacService _rbac;

    public IndexModel(IDocumentRepository documents, IRbacService rbac)
    {
        _documents = documents;
        _rbac = rbac;
    }

    public IReadOnlyList<RoleDefinition> Roles { get; private set; } = Array.Empty<RoleDefinition>();
    public IReadOnlyList<string> CurrentPermissions { get; private set; } = Array.Empty<string>();
    public int VisibleDocumentCount { get; private set; }

    public async Task OnGetAsync()
    {
        Roles = _rbac.Roles;
        CurrentPermissions = User.Identity?.IsAuthenticated == true
            ? _rbac.GetPermissions(User)
            : Array.Empty<string>();

        var documents = await _documents.GetAllAsync();
        VisibleDocumentCount = documents.Count(document =>
            User.Identity?.IsAuthenticated == true && _rbac.CanViewDocument(User, document));
    }
}
