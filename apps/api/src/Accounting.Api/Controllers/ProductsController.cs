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
[Route("api/organizations/{orgId:guid}/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService              _service;
    private readonly IAuditService                _audit;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    public ProductsController(
        IProductService service,
        IAuditService audit,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _service         = service;
        _audit           = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(Guid orgId, [FromBody] CreateProductDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ProductCreated, "Product", result.Id,
            $"Creó el producto/servicio \"{result.Name}\"", ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid orgId, Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.UpdateAsync(orgId, id, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ProductUpdated, "Product", id,
            $"Editó el producto/servicio \"{result.Name}\"", ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _service.DeleteAsync(orgId, id, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.ProductDeleted, "Product", id,
            "Eliminó el producto/servicio.", ct);
        return NoContent();
    }
}
