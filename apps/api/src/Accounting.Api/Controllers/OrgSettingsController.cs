using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations/{orgId:guid}/settings")]
public class OrgSettingsController : ControllerBase
{
    private readonly IOrgSettingsService _service;
    public OrgSettingsController(IOrgSettingsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<OrgSettingsDto>> Get(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAsync(orgId, ct));

    [HttpPut]
    public async Task<ActionResult<OrgSettingsDto>> Upsert(
        Guid orgId, [FromBody] UpdateOrgSettingsDto dto, CancellationToken ct) =>
        Ok(await _service.UpsertAsync(orgId, dto, ct));
}
