using Accounting.Api.Filters;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
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
    public AccountsController(IAccountService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> List(Guid orgId, CancellationToken ct)
        => Ok(await _service.ListAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(
        Guid orgId, [FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(orgId, dto, ct);
        return CreatedAtAction(nameof(List), new { orgId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(
        Guid orgId, Guid id, [FromBody] UpdateAccountDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(orgId, id, dto, ct));

    [HttpPatch("{id:guid}/toggle")]
    public async Task<ActionResult<AccountDto>> Toggle(Guid orgId, Guid id, CancellationToken ct)
        => Ok(await _service.ToggleActiveAsync(orgId, id, ct));
}
