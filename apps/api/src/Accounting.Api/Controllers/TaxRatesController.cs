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
[Route("api/organizations/{orgId:guid}/tax-rates")]
public class TaxRatesController : ControllerBase
{
    private readonly ITaxRateService              _service;
    private readonly IAuditService                _audit;
    private readonly IValidator<CreateTaxRateDto> _createValidator;

    public TaxRatesController(
        ITaxRateService service,
        IAuditService audit,
        IValidator<CreateTaxRateDto> createValidator)
    {
        _service         = service;
        _audit           = audit;
        _createValidator = createValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<TaxRateDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<TaxRateDto>> Create(Guid orgId, [FromBody] CreateTaxRateDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.TaxRateCreated, "TaxRate", result.Id,
            $"Creó la tasa de impuesto \"{result.Name}\" ({result.Rate:P2})", ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> Update(Guid orgId, Guid id, [FromBody] UpdateTaxRateDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        var result = await _service.UpdateAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.TaxRateUpdated, "TaxRate", id,
            $"Editó la tasa de impuesto \"{result.Name}\"", ct);
        return Ok(result);
    }
}
