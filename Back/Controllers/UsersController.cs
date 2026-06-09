using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IDirectoryService _directory;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;

    public UsersController(IDirectoryService directory, IRbacService rbac, IAuditService audit)
    {
        _directory = directory;
        _rbac = rbac;
        _audit = audit;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista usuarios LDAP.",
        Description = "Apenas Administrador. Consulta usuarios no servidor LDAP configurado e retorna roles sem senha.")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll()
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        return Ok((await _directory.GetUsersAsync()).Select(user => user.ToDto()).ToList());
    }

    [HttpPut("{userName}/role")]
    [SwaggerOperation(
        Summary = "Altera papel no LDAP.",
        Description = "Apenas Administrador. Atualiza o atributo de papel do usuario no LDAP. A nova permissao vale no proximo login.")]
    public async Task<ActionResult<MessageResponseDto>> UpdateRole(string userName, UpdateRoleRequestDto request)
    {
        return await UpdateRoleCore(userName, request.Role);
    }

    [HttpPost("role")]
    [SwaggerOperation(
        Summary = "Altera papel por formulario HTML.",
        Description = "Apenas Administrador. Rota auxiliar em POST para front estatico sem JavaScript.")]
    public async Task<IActionResult> UpdateRoleFromForm([FromForm] string userName, [FromForm] string role)
    {
        var result = await UpdateRoleCore(userName, role);
        if (result.Result is OkObjectResult)
        {
            return Redirect("/dashboard");
        }

        return result.Result ?? Ok(result.Value);
    }

    private async Task<ActionResult<MessageResponseDto>> UpdateRoleCore(string userName, string role)
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        var allowedRoles = _rbac.Roles
            .Where(role => role.Name != AppRoles.Service)
            .Select(role => role.Name)
            .ToHashSet();

        if (!allowedRoles.Contains(role))
        {
            return BadRequest(new MessageResponseDto { Message = "Papel invalido." });
        }

        await _directory.UpdateRoleAsync(userName, role);
        await _audit.WriteAsync("directory.role.changed", User.Identity?.Name ?? "unknown", $"{userName} recebeu o papel {role} via API.");
        return Ok(new MessageResponseDto { Message = $"Papel de {userName} atualizado para {role}." });
    }
}
