using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IBudgetRepository
{
    Task<List<Budget>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<Budget?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task               AddAsync(Budget budget, CancellationToken ct = default);
    void               RemoveLine(BudgetLine line);
    Task               SaveChangesAsync(CancellationToken ct = default);
}
