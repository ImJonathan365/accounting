using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public interface ITaxRateService
{
    Task<List<TaxRateDto>> GetAllAsync(Guid orgId, CancellationToken ct = default);
    Task<TaxRateDto>       GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<TaxRateDto>       CreateAsync(Guid orgId, CreateTaxRateDto dto, CancellationToken ct = default);
    Task<TaxRateDto>       UpdateAsync(Guid orgId, Guid id, UpdateTaxRateDto dto, CancellationToken ct = default);
}

public class TaxRateService : ITaxRateService
{
    private readonly ITaxRateRepository _repo;
    public TaxRateService(ITaxRateRepository repo) => _repo = repo;

    public async Task<List<TaxRateDto>> GetAllAsync(Guid orgId, CancellationToken ct = default) =>
        (await _repo.GetByOrganizationAsync(orgId, ct)).Select(Map).ToList();

    public async Task<TaxRateDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Tasa de impuesto no encontrada.");
        return Map(t);
    }

    public async Task<TaxRateDto> CreateAsync(Guid orgId, CreateTaxRateDto dto, CancellationToken ct = default)
    {
        var taxRate = new TaxRate
        {
            OrganizationId = orgId,
            Name           = dto.Name.Trim(),
            Rate           = dto.Rate,
            TaxAccountId   = dto.TaxAccountId,
        };
        await _repo.AddAsync(taxRate, ct);
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdAsync(orgId, taxRate.Id, ct);
        return Map(full!);
    }

    public async Task<TaxRateDto> UpdateAsync(Guid orgId, Guid id, UpdateTaxRateDto dto, CancellationToken ct = default)
    {
        var taxRate = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Tasa de impuesto no encontrada.");
        if (dto.Name         is not null) taxRate.Name         = dto.Name.Trim();
        if (dto.Rate         is not null) taxRate.Rate         = dto.Rate.Value;
        if (dto.TaxAccountId is not null) taxRate.TaxAccountId = dto.TaxAccountId.Value;
        if (dto.IsActive     is not null) taxRate.IsActive     = dto.IsActive.Value;
        await _repo.SaveChangesAsync(ct);
        return Map(taxRate);
    }

    private static TaxRateDto Map(TaxRate t) =>
        new(t.Id, t.Name, t.Rate, t.TaxAccountId, t.TaxAccount?.Name ?? "", t.IsActive);
}
