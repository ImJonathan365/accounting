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
[Route("api/organizations/{orgId:guid}/tax-rates")]
public class TaxRatesController : ControllerBase
{
    private readonly ITaxRateService              _service;
    private readonly IValidator<CreateTaxRateDto> _createValidator;

    public TaxRatesController(ITaxRateService service, IValidator<CreateTaxRateDto> createValidator)
    {
        _service         = service;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaxRateDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<TaxRateDto>> Create(Guid orgId, [FromBody] CreateTaxRateDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.CreateAsync(orgId, dto, ct));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> Update(Guid orgId, Guid id, [FromBody] UpdateTaxRateDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.UpdateAsync(orgId, id, dto, ct));
    }
}
