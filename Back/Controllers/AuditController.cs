using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[Authorize]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _audit;
    private readonly IRbacService _rbac;

    public AuditController(IAuditService audit, IRbacService rbac)
    {
        _audit = audit;
        _rbac = rbac;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditRecordDto>>> GetRecent([FromQuery] int limit = 80)
    {
        if (!_rbac.HasPermission(User, Permissions.ViewAudit))
        {
            return Forbid();
        }

        var safeLimit = Math.Clamp(limit, 1, 200);
        return Ok((await _audit.GetRecentAsync(safeLimit)).Select(record => record.ToDto()).ToList());
    }
}
