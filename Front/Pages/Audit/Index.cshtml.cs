using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Audit;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAuditService _audit;
    private readonly IRbacService _rbac;

    public IndexModel(IAuditService audit, IRbacService rbac)
    {
        _audit = audit;
        _rbac = rbac;
    }

    public IReadOnlyList<AuditRecord> Records { get; private set; } = Array.Empty<AuditRecord>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_rbac.HasPermission(User, Permissions.ViewAudit))
        {
            return Forbid();
        }

        Records = await _audit.GetRecentAsync();
        return Page();
    }
}
