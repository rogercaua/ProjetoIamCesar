using System.Security.Claims;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IDirectoryService _directory;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;

    public AuthController(IDirectoryService directory, IRbacService rbac, IAuditService audit)
    {
        _directory = directory;
        _rbac = rbac;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(LoginRequestDto request)
    {
        var user = await _directory.AuthenticateAsync(request.UserName.Trim(), request.Password);
        if (user is null)
        {
            await _audit.WriteAsync("ldap.login.failed", request.UserName, "Falha de autenticacao via API.");
            return Unauthorized(new MessageResponseDto { Message = "Usuario ou senha invalidos." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        await _audit.WriteAsync("ldap.login.success", user.UserName, $"Login LDAP via API com papel: {string.Join(", ", user.Roles)}.");

        return Ok(user.ToAuthenticatedDto(_rbac.GetPermissions(principal)));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<AuthenticatedUserDto> Me()
    {
        return Ok(new AuthenticatedUserDto
        {
            UserName = User.Identity?.Name ?? "",
            DisplayName = User.FindFirstValue("display_name") ?? "",
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
            Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList(),
            Permissions = _rbac.GetPermissions(User)
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<MessageResponseDto>> Logout()
    {
        var actor = User.Identity?.Name ?? "anonymous";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _audit.WriteAsync("session.logout", actor, "Sessao encerrada via API.");
        return Ok(new MessageResponseDto { Message = "Sessao encerrada." });
    }
}
