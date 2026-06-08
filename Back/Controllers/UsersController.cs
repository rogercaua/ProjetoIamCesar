using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll()
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        return Ok((await _directory.GetUsersAsync()).Select(user => user.ToDto()).ToList());
    }

    [HttpPut("{userName}/role")]
    public async Task<ActionResult<MessageResponseDto>> UpdateRole(string userName, UpdateRoleRequestDto request)
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        var allowedRoles = _rbac.Roles
            .Where(role => role.Name != AppRoles.Service)
            .Select(role => role.Name)
            .ToHashSet();

        if (!allowedRoles.Contains(request.Role))
        {
            return BadRequest(new MessageResponseDto { Message = "Papel invalido." });
        }

        await _directory.UpdateRoleAsync(userName, request.Role);
        await _audit.WriteAsync("directory.role.changed", User.Identity?.Name ?? "unknown", $"{userName} recebeu o papel {request.Role} via API.");
        return Ok(new MessageResponseDto { Message = $"Papel de {userName} atualizado para {request.Role}." });
    }
}
