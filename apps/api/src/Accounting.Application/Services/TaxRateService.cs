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
    Task                   DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class TaxRateService : ITaxRateService
{
    private readonly ITaxRateRepository  _repo;
    private readonly IAccountRepository  _accounts;

    public TaxRateService(ITaxRateRepository repo, IAccountRepository accounts)
    {
        _repo     = repo;
        _accounts = accounts;
    }

    public async Task<List<TaxRateDto>> GetAllAsync(Guid orgId, CancellationToken ct = default) =>
        (await _repo.GetByOrganizationAsync(orgId, ct)).Select(Map).ToList();

    public async Task<TaxRateDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Tasa de impuesto no encontrada.");
        return Map(t);
    }

    public async Task<TaxRateDto> CreateAsync(Guid orgId, CreateTaxRateDto dto, CancellationToken ct = default)
    {
        if (await _repo.NameExistsAsync(orgId, dto.Name.Trim(), ct))
            throw new InvalidOperationException($"Ya existe una tasa de impuesto con el nombre \"{dto.Name.Trim()}\".");

        if (await _accounts.GetByIdAsync(dto.TaxAccountId, orgId, ct) is null)
            throw new ArgumentException("La cuenta de impuesto no pertenece a esta organización.");

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
        if (dto.Name is not null)
        {
            var newName = dto.Name.Trim();
            if (newName != taxRate.Name && await _repo.NameExistsAsync(orgId, newName, ct))
                throw new InvalidOperationException($"Ya existe una tasa de impuesto con el nombre \"{newName}\".");
            taxRate.Name = newName;
        }
        if (dto.Rate         is not null) taxRate.Rate         = dto.Rate.Value;
        if (dto.TaxAccountId is not null)
        {
            if (await _accounts.GetByIdAsync(dto.TaxAccountId.Value, orgId, ct) is null)
                throw new ArgumentException("La cuenta de impuesto no pertenece a esta organización.");
            taxRate.TaxAccountId = dto.TaxAccountId.Value;
        }
        if (dto.IsActive     is not null) taxRate.IsActive     = dto.IsActive.Value;
        await _repo.SaveChangesAsync(ct);
        var updated = await _repo.GetByIdAsync(orgId, id, ct);
        return Map(updated!);
    }

    public async Task DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var taxRate = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Tasa de impuesto no encontrada.");
        if (await _repo.IsUsedAsync(orgId, id, ct))
            throw new InvalidOperationException("No se puede eliminar la tasa de impuesto porque está en uso en productos o facturas.");
        _repo.Remove(taxRate);
        await _repo.SaveChangesAsync(ct);
    }

    private static TaxRateDto Map(TaxRate t) =>
        new(t.Id, t.Name, t.Rate, t.TaxAccountId, t.TaxAccount?.Name ?? "", t.IsActive);
}
