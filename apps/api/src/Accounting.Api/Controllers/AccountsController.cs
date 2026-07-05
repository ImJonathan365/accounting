using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;
    private readonly IAuditService   _audit;

    public AccountsController(IAccountService service, IAuditService audit)
    {
        _service = service;
        _audit   = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> List(Guid orgId, CancellationToken ct)
        => Ok(await _service.ListAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(
        Guid orgId, [FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var created = await _service.CreateAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.AccountCreated, "Account", created.Id,
            $"Creó la cuenta {created.Code} — {created.Name}", ct);
        return CreatedAtAction(nameof(List), new { orgId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(
        Guid orgId, Guid id, [FromBody] UpdateAccountDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var updated = await _service.UpdateAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.AccountUpdated, "Account", id,
            $"Editó la cuenta {updated.Code} — {updated.Name}", ct);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<ActionResult<AccountDto>> Toggle(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin"))
            return Forbid();

        var toggled = await _service.ToggleActiveAsync(orgId, id, ct);
        var verb = toggled.IsActive ? "Activó" : "Desactivó";
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.AccountToggled, "Account", id,
            $"{verb} la cuenta {toggled.Code} — {toggled.Name}", ct);
        return Ok(toggled);
    }
}
