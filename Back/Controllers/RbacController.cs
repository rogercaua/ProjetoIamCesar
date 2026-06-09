using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
        Summary = "Mostra a matriz RBAC.",
        Description = "Publico. Lista os papeis da aplicacao e suas permissoes.")]
    public ActionResult<IReadOnlyList<RoleDto>> GetRoles()
    {
        return Ok(_rbac.Roles.Select(role => role.ToDto()).ToList());
    }
}
