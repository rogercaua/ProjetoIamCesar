using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Route("api/rbac")]
public sealed class RbacController : ControllerBase
{
    private readonly IRbacService _rbac;

    public RbacController(IRbacService rbac)
    {
        _rbac = rbac;
    }

    [HttpGet("roles")]
    public ActionResult<IReadOnlyList<RoleDto>> GetRoles()
    {
        return Ok(_rbac.Roles.Select(role => role.ToDto()).ToList());
    }
}
