using System.Text.Json;
using System.Security.Claims;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
        Summary = "Login LDAP real.",
        Description = "Publico. Autentica no servidor LDAP configurado, cria cookie de sessao e retorna papeis/permissoes RBAC.")]
    public async Task<ActionResult<AuthenticatedUserDto>> Login()
    {
        var request = await ReadLoginRequestAsync();
        var user = await _directory.AuthenticateAsync(request.UserName.Trim(), request.Password);
        if (user is null)
        {
            await _audit.WriteAsync("ldap.login.failed", request.UserName, "Falha de autenticacao via API.");
            if (IsHtmlFormRequest())
            {
                return Redirect("/?erro=login");
            }

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

        if (IsHtmlFormRequest())
        {
            return Redirect("/dashboard");
        }

        return Ok(user.ToAuthenticatedDto(_rbac.GetPermissions(principal)));
    }

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Consulta a sessao atual.",
        Description = "Usuario logado. Retorna usuario, e-mail, roles LDAP e permissoes RBAC carregadas no cookie.")]
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
    [SwaggerOperation(
        Summary = "Encerra sessao.",
        Description = "Usuario logado. Remove o cookie de autenticacao da aplicacao.")]
    public async Task<ActionResult<MessageResponseDto>> Logout()
    {
        var actor = User.Identity?.Name ?? "anonymous";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _audit.WriteAsync("session.logout", actor, "Sessao encerrada via API.");

        if (IsHtmlFormRequest())
        {
            return Redirect("/");
        }

        return Ok(new MessageResponseDto { Message = "Sessao encerrada." });
    }

    private async Task<LoginRequestDto> ReadLoginRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return new LoginRequestDto
            {
                UserName = form["userName"].ToString(),
                Password = form["password"].ToString()
            };
        }

        return await JsonSerializer.DeserializeAsync<LoginRequestDto>(
            Request.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new LoginRequestDto();
    }

    private bool IsHtmlFormRequest()
    {
        return Request.HasFormContentType
            || Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
