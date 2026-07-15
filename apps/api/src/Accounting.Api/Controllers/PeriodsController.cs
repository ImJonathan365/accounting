using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
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
    private readonly IAuditService              _audit;
    private readonly IValidator<ClosePeriodDto> _closeValidator;

    public PeriodsController(IPeriodService service, IAuditService audit, IValidator<ClosePeriodDto> closeValidator)
    {
        _service        = service;
        _audit          = audit;
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
        var result = await _service.CloseAsync(orgId, userId, dto, ct);
        await _audit.LogAsync(orgId, userId, AuditActions.PeriodClosed, "AccountingPeriod", Guid.Empty,
            $"Cerró período {dto.Year}-{dto.Month:D2}", ct);
        return Ok(result);
    }

    [HttpDelete("{year:int}/{month:int}")]
    public async Task<IActionResult> Reopen(Guid orgId, int year, int month, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner"))
            return Forbid();

        await _service.ReopenAsync(orgId, year, month, ct);
        var userId = OrgAuth.GetUserId(HttpContext);
        await _audit.LogAsync(orgId, userId, AuditActions.PeriodReopened, "AccountingPeriod", Guid.Empty,
            $"Reabrió período {year}-{month:D2}", ct);
        return NoContent();
    }
}
