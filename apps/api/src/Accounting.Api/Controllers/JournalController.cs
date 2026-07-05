using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/journal-entries")]
public class JournalController : ControllerBase
{
    private readonly IJournalService _service;
    private readonly IAuditService   _audit;

    public JournalController(IJournalService service, IAuditService audit)
    {
        _service = service;
        _audit   = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<JournalEntrySummaryDto>>> List(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        JournalStatus? statusFilter = status switch
        {
            "Posted" => JournalStatus.Posted,
            "Voided" => JournalStatus.Voided,
            "Draft"  => JournalStatus.Draft,
            _        => null
        };

        return Ok(await _service.ListAsync(orgId, page, pageSize, from, to, statusFilter, search?.Trim(), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> Get(Guid orgId, Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, orgId, ct));

    [HttpPost]
    public async Task<ActionResult<JournalEntryDto>> Create(
        Guid orgId, [FromBody] CreateJournalEntryDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(orgId, dto, ct);
        var action  = dto.IsDraft ? AuditActions.JournalDraftCreated : AuditActions.JournalPostedCreated;
        var verb    = dto.IsDraft ? "Creó borrador" : "Registró asiento";
        await _audit.LogAsync(orgId, CurrentUserId, action, "JournalEntry", created.Id,
            $"{verb}: \"{created.Description}\"", ct);
        return CreatedAtAction(nameof(Get), new { orgId, id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> Update(
        Guid orgId, Guid id, [FromBody] UpdateJournalEntryDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.JournalUpdated, "JournalEntry", id,
            $"Editó borrador: \"{updated.Description}\"", ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        // Get description before deleting for the audit log
        var entry = await _service.GetByIdAsync(id, orgId, ct);
        await _service.DeleteAsync(orgId, id, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.JournalDeleted, "JournalEntry", id,
            $"Eliminó borrador: \"{entry.Description}\"", ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<JournalEntryDto>> Post(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var posted = await _service.PostAsync(orgId, id, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.JournalPosted, "JournalEntry", id,
            $"Registró asiento: \"{posted.Description}\"", ct);
        return Ok(posted);
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<JournalEntryDto>> Void(
        Guid orgId, Guid id, [FromBody] VoidJournalEntryDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var voided = await _service.VoidAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.JournalVoided, "JournalEntry", id,
            $"Anuló asiento: \"{voided.Description}\"", ct);
        return Ok(voided);
    }
}
