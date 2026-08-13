using System.Security.Claims;
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
[Route("api/organizations/{orgId:guid}/opening-balances")]
public class OpeningBalancesController : ControllerBase
{
    private readonly IOpeningBalanceService                  _service;
    private readonly IAuditService                           _audit;
    private readonly IValidator<SetOpeningBalancesRequest>   _validator;

    public OpeningBalancesController(
        IOpeningBalanceService service,
        IAuditService audit,
        IValidator<SetOpeningBalancesRequest> validator)
    {
        _service   = service;
        _audit     = audit;
        _validator = validator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<OpeningBalanceResultDto>> Set(
        Guid orgId, [FromBody] SetOpeningBalancesRequest dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _validator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.SetAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.OpeningBalancesSet, "OpeningBalance", result.JournalEntryId,
            $"Estableció saldos iniciales — débito {result.TotalDebit:F2}, crédito {result.TotalCredit:F2}", ct);
        return Ok(result);
    }
}
