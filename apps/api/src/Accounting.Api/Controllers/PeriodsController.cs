using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/periods")]
public class PeriodsController : ControllerBase
{
    private readonly IPeriodService             _service;
    private readonly IValidator<ClosePeriodDto> _closeValidator;

    public PeriodsController(IPeriodService service, IValidator<ClosePeriodDto> closeValidator)
    {
        _service        = service;
        _closeValidator = closeValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<PeriodDto>>> List(
        Guid orgId, [FromQuery] int? year, CancellationToken ct)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return Ok(await _service.ListAsync(orgId, y, ct));
    }

    [HttpPost("close")]
    public async Task<ActionResult<PeriodDto>> Close(
        Guid orgId, [FromBody] ClosePeriodDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        await _closeValidator.ValidateAndThrowAsync(dto, ct);
        var userId = OrgAuth.GetUserId(HttpContext);
        return Ok(await _service.CloseAsync(orgId, userId, dto, ct));
    }

    [HttpDelete("{year:int}/{month:int}")]
    public async Task<IActionResult> Reopen(Guid orgId, int year, int month, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner"))
            return Forbid();

        await _service.ReopenAsync(orgId, year, month, ct);
        return NoContent();
    }
}
