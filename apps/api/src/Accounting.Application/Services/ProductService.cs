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
    Task                   DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IProductRepository  _repo;
    private readonly IAccountRepository  _accounts;
    private readonly ITaxRateRepository  _taxRates;

    public ProductService(IProductRepository repo, IAccountRepository accounts, ITaxRateRepository taxRates)
    {
        _repo     = repo;
        _accounts = accounts;
        _taxRates = taxRates;
    }

    public async Task<List<ProductDto>> GetAllAsync(Guid orgId, CancellationToken ct = default) =>
        (await _repo.GetByOrganizationAsync(orgId, ct)).Select(Map).ToList();

    public async Task<ProductDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        return Map(p);
    }

    public async Task<ProductDto> CreateAsync(Guid orgId, CreateProductDto dto, CancellationToken ct = default)
    {
        if (await _repo.NameExistsAsync(orgId, dto.Name.Trim(), ct))
            throw new InvalidOperationException($"Ya existe un producto con el nombre \"{dto.Name.Trim()}\".");

        if (await _accounts.GetByIdAsync(dto.AccountId, orgId, ct) is null)
            throw new ArgumentException("La cuenta de ingreso/gasto no pertenece a esta organización.");

        if (dto.TaxRateId.HasValue && await _taxRates.GetByIdAsync(orgId, dto.TaxRateId.Value, ct) is null)
            throw new ArgumentException("La tasa de impuesto no pertenece a esta organización.");

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
        if (dto.Name is not null)
        {
            var newName = dto.Name.Trim();
            if (newName != product.Name && await _repo.NameExistsAsync(orgId, newName, ct))
                throw new InvalidOperationException($"Ya existe un producto con el nombre \"{newName}\".");
            product.Name = newName;
        }
        if (dto.Description  is not null) product.Description  = dto.Description.Trim();
        if (dto.DefaultPrice is not null) product.DefaultPrice = dto.DefaultPrice.Value;
        if (dto.AccountId is not null)
        {
            if (await _accounts.GetByIdAsync(dto.AccountId.Value, orgId, ct) is null)
                throw new ArgumentException("La cuenta de ingreso/gasto no pertenece a esta organización.");
            product.AccountId = dto.AccountId.Value;
        }
        if (dto.IsActive     is not null) product.IsActive     = dto.IsActive.Value;
        // Guid.Empty = explicit "clear"; any other Guid = set; null = not provided (skip)
        if (dto.TaxRateId.HasValue)
        {
            if (dto.TaxRateId != Guid.Empty && await _taxRates.GetByIdAsync(orgId, dto.TaxRateId.Value, ct) is null)
                throw new ArgumentException("La tasa de impuesto no pertenece a esta organización.");
            product.TaxRateId = dto.TaxRateId == Guid.Empty ? (Guid?)null : dto.TaxRateId;
        }
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdAsync(orgId, product.Id, ct);
        return Map(full!);
    }

    public async Task DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Producto no encontrado.");
        _repo.Remove(product);
        await _repo.SaveChangesAsync(ct);
    }

    private static ProductDto Map(Product p) => new(
        p.Id, p.Name, p.Description, p.DefaultPrice,
        p.AccountId, p.Account?.Code ?? "", p.Account?.Name ?? "",
        p.TaxRateId, p.TaxRate?.Name, p.TaxRate?.Rate ?? 0,
        p.IsActive);
}
