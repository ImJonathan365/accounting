using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(Guid orgId, CancellationToken ct = default);
    Task<ProductDto>       GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<ProductDto>       CreateAsync(Guid orgId, CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDto>       UpdateAsync(Guid orgId, Guid id, UpdateProductDto dto, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    public ProductService(IProductRepository repo) => _repo = repo;

    public async Task<List<ProductDto>> GetAllAsync(Guid orgId, CancellationToken ct = default) =>
        (await _repo.GetByOrganizationAsync(orgId, ct)).Select(Map).ToList();

    public async Task<ProductDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        return Map(p);
    }

    public async Task<ProductDto> CreateAsync(Guid orgId, CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            OrganizationId = orgId,
            Name           = dto.Name.Trim(),
            Description    = dto.Description?.Trim(),
            DefaultPrice   = dto.DefaultPrice,
            AccountId      = dto.AccountId,
            TaxRateId      = dto.TaxRateId,
        };
        await _repo.AddAsync(product, ct);
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdAsync(orgId, product.Id, ct);
        return Map(full!);
    }

    public async Task<ProductDto> UpdateAsync(Guid orgId, Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        if (dto.Name         is not null) product.Name         = dto.Name.Trim();
        if (dto.Description  is not null) product.Description  = dto.Description.Trim();
        if (dto.DefaultPrice is not null) product.DefaultPrice = dto.DefaultPrice.Value;
        if (dto.AccountId    is not null) product.AccountId    = dto.AccountId.Value;
        if (dto.IsActive     is not null) product.IsActive     = dto.IsActive.Value;
        // TaxRateId can be set to null explicitly
        if (dto.TaxRateId.HasValue || dto.TaxRateId == null && dto.IsActive is not null)
            product.TaxRateId = dto.TaxRateId;
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdAsync(orgId, product.Id, ct);
        return Map(full!);
    }

    private static ProductDto Map(Product p) => new(
        p.Id, p.Name, p.Description, p.DefaultPrice,
        p.AccountId, p.Account?.Code ?? "", p.Account?.Name ?? "",
        p.TaxRateId, p.TaxRate?.Name, p.TaxRate?.Rate ?? 0,
        p.IsActive);
}
