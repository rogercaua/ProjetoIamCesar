using System.Security.Claims;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IDirectoryService _directory;
    private readonly IAuditService _audit;

    public LoginModel(IDirectoryService directory, IAuditService audit)
    {
        _directory = directory;
        _audit = audit;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty]
    public string ReturnUrl { get; set; } = "/Documents";

    public string? ErrorMessage { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Documents" : returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _directory.AuthenticateAsync(Input.UserName.Trim(), Input.Password);
        if (user is null)
        {
            ErrorMessage = "Usuario ou senha invalidos no LDAP demo.";
            await _audit.WriteAsync("ldap.login.failed", Input.UserName, "Falha de autenticacao.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        await _audit.WriteAsync("ldap.login.success", user.UserName, $"Login LDAP com papel: {string.Join(", ", user.Roles)}.");
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/Documents");
    }

    public sealed class LoginInput
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
