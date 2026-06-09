using System.Security.Claims;
using DocumentPortalIam.Back.Core.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Authorize]
[Route("api/google")]
public sealed class GoogleController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public GoogleController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("connect")]
    [SwaggerOperation(
        Summary = "Conecta conta Google via OpenID Connect.",
        Description = "Usuario logado. Redireciona para o Google usando OIDC e solicita escopo drive.file para exportar documentos.")]
    public IActionResult Connect([FromQuery] string returnUrl = "/")
    {
        if (IsMissingGoogleValue(_configuration["Google:ClientId"])
            || IsMissingGoogleValue(_configuration["Google:ClientSecret"]))
        {
            return BadRequest(new MessageResponseDto
            {
                Message = "Configure Google:ClientId e Google:ClientSecret no appsettings antes de conectar."
            });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        };

        return Challenge(properties, "Google");
    }

    [HttpGet("status")]
    [SwaggerOperation(
        Summary = "Mostra status da conexao Google.",
        Description = "Usuario logado. Verifica se existe cookie externo com tokens OIDC/Google.")]
    public async Task<ActionResult<GoogleConnectionDto>> Status()
    {
        var result = await HttpContext.AuthenticateAsync("GoogleExternal");
        if (!result.Succeeded || result.Principal is null)
        {
            return Ok(new GoogleConnectionDto());
        }

        return Ok(new GoogleConnectionDto
        {
            Connected = true,
            Email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "",
            Name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? ""
        });
    }

    [HttpPost("disconnect")]
    [SwaggerOperation(
        Summary = "Desconecta conta Google.",
        Description = "Usuario logado. Remove o cookie externo usado para guardar os tokens OIDC/Google.")]
    public async Task<ActionResult<MessageResponseDto>> Disconnect()
    {
        await HttpContext.SignOutAsync("GoogleExternal");
        if (Request.HasFormContentType
            || Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/dashboard");
        }

        return Ok(new MessageResponseDto { Message = "Conta Google desconectada." });
    }

    private static bool IsMissingGoogleValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.StartsWith("SEU_", StringComparison.OrdinalIgnoreCase);
    }
}
