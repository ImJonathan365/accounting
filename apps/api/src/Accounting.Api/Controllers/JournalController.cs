using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations/{orgId:guid}/journal-entries")]
public class JournalController : ControllerBase
{
    private readonly IJournalService _service;
    public JournalController(IJournalService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<JournalEntrySummaryDto>>> List(Guid orgId, CancellationToken ct)
        => Ok(await _service.ListAsync(orgId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> Get(Guid orgId, Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, orgId, ct));

    [HttpPost]
    public async Task<ActionResult<JournalEntryDto>> Create(
        Guid orgId, [FromBody] CreateJournalEntryDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(orgId, dto, ct);
        return CreatedAtAction(nameof(Get), new { orgId, id = created.Id }, created);
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<JournalEntryDto>> Void(
        Guid orgId, Guid id, [FromBody] VoidJournalEntryDto dto, CancellationToken ct)
        => Ok(await _service.VoidAsync(orgId, id, dto, ct));
}
