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
[Route("api/organizations/{orgId:guid}/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService                    _service;
    private readonly IAuditService                     _audit;
    private readonly IValidator<CreateBudgetDto>       _createValidator;
    private readonly IValidator<UpsertBudgetLineDto>   _lineValidator;

    public BudgetsController(
        IBudgetService service,
        IAuditService audit,
        IValidator<CreateBudgetDto> createValidator,
        IValidator<UpsertBudgetLineDto> lineValidator)
    {
        _service         = service;
        _audit           = audit;
        _createValidator = createValidator;
        _lineValidator   = lineValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        var result = await _service.CreateAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BudgetCreated, "Budget", result.Id,
            $"Creó presupuesto \"{result.Name}\" {result.Year}", ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/lines")]
    public async Task<ActionResult<BudgetDto>> UpsertLine(Guid orgId, Guid id, [FromBody] UpsertBudgetLineDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _lineValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.UpsertLineAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BudgetLineUpserted, "Budget", id,
            $"Actualizó línea de presupuesto (cuenta {dto.AccountId})", ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/vs-actual")]
    public async Task<ActionResult<BudgetVsActualDto>> GetVsActual(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetVsActualAsync(orgId, id, ct));
}
