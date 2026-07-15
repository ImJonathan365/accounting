using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface ITaxRateRepository
{
    Task<List<TaxRate>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<TaxRate?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<List<TaxRate>> GetByIdsAsync(IEnumerable<Guid> ids, Guid orgId, CancellationToken ct = default);
    Task<bool>          NameExistsAsync(Guid orgId, string name, CancellationToken ct = default);
    Task                AddAsync(TaxRate taxRate, CancellationToken ct = default);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
