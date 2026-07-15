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
[Route("api/organizations/{orgId:guid}/year-end")]
public class YearEndController : ControllerBase
{
    private readonly IYearEndClosingService             _service;
    private readonly IAuditService                      _audit;
    private readonly IValidator<YearEndCloseRequestDto> _validator;

    public YearEndController(
        IYearEndClosingService service,
        IAuditService audit,
        IValidator<YearEndCloseRequestDto> validator)
    {
        _service   = service;
        _audit     = audit;
        _validator = validator;
    }

    [HttpGet("{year:int}")]
    public async Task<ActionResult<YearEndStatusDto>> GetStatus(Guid orgId, int year, CancellationToken ct) =>
        Ok(await _service.GetStatusAsync(orgId, year, ct));

    [HttpPost("close")]
    public async Task<ActionResult<YearEndStatusDto>> Close(
        Guid orgId, [FromBody] YearEndCloseRequestDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner"))
            return Forbid();

        await _validator.ValidateAndThrowAsync(dto, ct);
        var userId = OrgAuth.GetUserId(HttpContext);
        var result = await _service.CloseYearAsync(orgId, userId, dto, ct);
        await _audit.LogAsync(orgId, userId, AuditActions.YearEndClosed, "YearEndClosing", result.JournalEntryId ?? Guid.Empty,
            $"Realizó cierre de ejercicio {dto.Year}", ct);
        return Ok(result);
    }
}
