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
[Route("api/organizations/{orgId:guid}/recurring-entries")]
public class RecurringEntriesController : ControllerBase
{
    private readonly IRecurringEntryService              _service;
    private readonly IAuditService                       _audit;
    private readonly IValidator<CreateRecurringEntryDto> _createValidator;
    private readonly IValidator<UpdateRecurringEntryDto> _updateValidator;

    public RecurringEntriesController(
        IRecurringEntryService service,
        IAuditService audit,
        IValidator<CreateRecurringEntryDto> createValidator,
        IValidator<UpdateRecurringEntryDto> updateValidator)
    {
        _service         = service;
        _audit           = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<RecurringEntryDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurringEntryDto>> GetById(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(orgId, id, ct));

    [HttpPost]
    public async Task<ActionResult<RecurringEntryDto>> Create(
        Guid orgId, [FromBody] CreateRecurringEntryDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { orgId, id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RecurringEntryDto>> Update(
        Guid orgId, Guid id, [FromBody] UpdateRecurringEntryDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.UpdateAsync(orgId, id, dto, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        await _service.DeleteAsync(orgId, id, ct);
        return NoContent();
    }

    [HttpPost("generate-pending")]
    public async Task<ActionResult<GeneratePendingResultDto>> GeneratePending(Guid orgId, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var result = await _service.GeneratePendingAsync(orgId, ct);
        if (result.Generated > 0)
            await _audit.LogAsync(orgId, CurrentUserId, AuditActions.RecurringGenerated, "RecurringEntry", Guid.Empty,
                $"Generó {result.Generated} asiento(s) recurrente(s)", ct);
        return Ok(result);
    }
}
