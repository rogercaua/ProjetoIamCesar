using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly IAuditService _audit;

    public LogoutModel(IAuditService audit)
    {
        _audit = audit;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actor = User.Identity?.Name ?? "anonymous";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _audit.WriteAsync("session.logout", actor, "Sessao encerrada.");
        return RedirectToPage("/Index");
    }
}
