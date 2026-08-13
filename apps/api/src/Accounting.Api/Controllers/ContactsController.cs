using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactService                _service;
    private readonly IAuditService                  _audit;
    private readonly IValidator<CreateContactDto>   _createValidator;
    private readonly IValidator<UpdateContactDto>   _updateValidator;

    public ContactsController(
        IContactService service,
        IAuditService audit,
        IValidator<CreateContactDto> createValidator,
        IValidator<UpdateContactDto> updateValidator)
    {
        _service         = service;
        _audit           = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContactDto>>> GetAll(
        Guid orgId,
        [FromQuery] ContactType? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await _service.GetPagedAsync(orgId, type, search, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> GetById(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(orgId, id, ct));

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create(Guid orgId, [FromBody] CreateContactDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ContactCreated, "Contact", result.Id,
            $"Creó el contacto \"{result.Name}\"", ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Update(Guid orgId, Guid id, [FromBody] UpdateContactDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.UpdateAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ContactUpdated, "Contact", id,
            $"Editó el contacto \"{result.Name}\"", ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _service.DeleteAsync(orgId, id, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ContactDeleted, "Contact", id,
            "Eliminó el contacto.", ct);
        return NoContent();
    }
}
