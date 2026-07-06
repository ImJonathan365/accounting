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
[Route("api/organizations/{orgId:guid}/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService                    _service;
    private readonly IValidator<CreateBudgetDto>       _createValidator;
    private readonly IValidator<UpsertBudgetLineDto>   _lineValidator;

    public BudgetsController(
        IBudgetService service,
        IValidator<CreateBudgetDto> createValidator,
        IValidator<UpsertBudgetLineDto> lineValidator)
    {
        _service         = service;
        _createValidator = createValidator;
        _lineValidator   = lineValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetDto>> GetById(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(orgId, id, ct));

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create(Guid orgId, [FromBody] CreateBudgetDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.CreateAsync(orgId, dto, ct));
    }

    [HttpPut("{id:guid}/lines")]
    public async Task<ActionResult<BudgetDto>> UpsertLine(Guid orgId, Guid id, [FromBody] UpsertBudgetLineDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _lineValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.UpsertLineAsync(orgId, id, dto, ct));
    }

    [HttpGet("{id:guid}/vs-actual")]
    public async Task<ActionResult<BudgetVsActualDto>> GetVsActual(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetVsActualAsync(orgId, id, ct));
}
