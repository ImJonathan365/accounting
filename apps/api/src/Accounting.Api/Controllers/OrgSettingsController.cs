using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/settings")]
public class OrgSettingsController : ControllerBase
{
    private readonly IOrgSettingsService _service;
    private readonly IAuditService       _audit;

    public OrgSettingsController(IOrgSettingsService service, IAuditService audit)
    {
        _service = service;
        _audit   = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<OrgSettingsDto>> Get(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAsync(orgId, ct));

    [HttpPut]
    public async Task<ActionResult<OrgSettingsDto>> Upsert(
        Guid orgId, [FromBody] UpdateOrgSettingsDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner"))
            return Forbid();

        var result = await _service.UpsertAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.OrgSettingsUpdated, "OrgSettings", orgId,
            $"Actualizó la configuración de la organización \"{result.CompanyName}\"", ct);
        return Ok(result);
    }
}
