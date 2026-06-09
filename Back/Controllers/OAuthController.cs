using System.Text.Json;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Route("api/oauth")]
public sealed class OAuthController : ControllerBase
{
    private readonly IM2MTokenService _tokens;
    private readonly IAuditService _audit;

    public OAuthController(IM2MTokenService tokens, IAuditService audit)
    {
        _tokens = tokens;
        _audit = audit;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Emite token OAuth2 M2M.",
        Description = "Cliente tecnico. Recebe client_id/client_secret e emite Bearer token para exportacao M2M.")]
    public async Task<ActionResult<TokenResponseDto>> Token()
    {
        var request = await ReadClientCredentialsAsync();
        var issued = _tokens.IssueToken(request.ClientId, request.ClientSecret);

        if (issued is null)
        {
            await _audit.WriteAsync("oauth2.token.denied", request.ClientId, "Credenciais M2M invalidas.");
            return Unauthorized(new MessageResponseDto { Message = "Credenciais M2M invalidas." });
        }

        await _audit.WriteAsync("oauth2.token.issued", request.ClientId, "Token M2M emitido via controller.");
        return Ok(issued);
    }

    private async Task<ClientCredentialsRequestDto> ReadClientCredentialsAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            return new ClientCredentialsRequestDto
            {
                ClientId = form["client_id"].ToString(),
                ClientSecret = form["client_secret"].ToString()
            };
        }

        return await JsonSerializer.DeserializeAsync<ClientCredentialsRequestDto>(Request.Body)
            ?? new ClientCredentialsRequestDto();
    }
}
