using Accounting.Api.Filters;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get(Guid orgId, CancellationToken ct)
        => Ok(await _service.GetSummaryAsync(orgId, ct));
}
