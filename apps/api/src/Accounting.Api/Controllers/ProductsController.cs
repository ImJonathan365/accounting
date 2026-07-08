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
[Route("api/organizations/{orgId:guid}/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService              _service;
    private readonly IValidator<CreateProductDto> _createValidator;

    public ProductsController(IProductService service, IValidator<CreateProductDto> createValidator)
    {
        _service         = service;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(Guid orgId, [FromBody] CreateProductDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.CreateAsync(orgId, dto, ct));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid orgId, Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.UpdateAsync(orgId, id, dto, ct));
    }
}
