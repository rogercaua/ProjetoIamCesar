using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocumentPortalIam.Front.Pages.Admin;

[Authorize]
public sealed class UsersModel : PageModel
{
    private readonly IDirectoryService _directory;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;

    public UsersModel(IDirectoryService directory, IRbacService rbac, IAuditService audit)
    {
        _directory = directory;
        _rbac = rbac;
        _audit = audit;
    }

    public IReadOnlyList<AppUser> Users { get; private set; } = Array.Empty<AppUser>();
    public IReadOnlyList<RoleDefinition> Roles { get; private set; } = Array.Empty<RoleDefinition>();
    public string? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRoleAsync(string userName, string role)
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return Forbid();
        }

        var allowedRoles = _rbac.Roles
            .Where(item => item.Name != AppRoles.Service)
            .Select(item => item.Name)
            .ToHashSet();

        if (!allowedRoles.Contains(role))
        {
            return BadRequest("Papel invalido.");
        }

        await _directory.UpdateRoleAsync(userName, role);
        await _audit.WriteAsync("directory.role.changed", User.Identity?.Name ?? "unknown", $"{userName} recebeu o papel {role}.");
        Message = $"Papel de {userName} atualizado para {role}.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Users = await _directory.GetUsersAsync();
        Roles = _rbac.Roles.Where(role => role.Name != AppRoles.Service).ToList();
    }
}
