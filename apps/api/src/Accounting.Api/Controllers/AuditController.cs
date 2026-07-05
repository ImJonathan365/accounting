using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;
    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner"))
            return Forbid();

        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return Ok(await _audit.ListAsync(orgId, page, pageSize, ct));
    }
}
