using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
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
    private readonly IValidator<CreateContactDto>   _createValidator;
    private readonly IValidator<UpdateContactDto>   _updateValidator;

    public ContactsController(IContactService service, IValidator<CreateContactDto> createValidator, IValidator<UpdateContactDto> updateValidator)
    {
        _service         = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetAll(
        Guid orgId, [FromQuery] ContactType? type, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, type, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> GetById(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(orgId, id, ct));

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create(Guid orgId, [FromBody] CreateContactDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Update(Guid orgId, Guid id, [FromBody] UpdateContactDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.UpdateAsync(orgId, id, dto, ct));
    }
}
