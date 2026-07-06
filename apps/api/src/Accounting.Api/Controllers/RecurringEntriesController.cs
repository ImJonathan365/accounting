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
[Route("api/organizations/{orgId:guid}/recurring-entries")]
public class RecurringEntriesController : ControllerBase
{
    private readonly IRecurringEntryService              _service;
    private readonly IValidator<CreateRecurringEntryDto> _createValidator;
    private readonly IValidator<UpdateRecurringEntryDto> _updateValidator;

    public RecurringEntriesController(
        IRecurringEntryService service,
        IValidator<CreateRecurringEntryDto> createValidator,
        IValidator<UpdateRecurringEntryDto> updateValidator)
    {
        _service         = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

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

        return Ok(await _service.GeneratePendingAsync(orgId, ct));
    }
}
