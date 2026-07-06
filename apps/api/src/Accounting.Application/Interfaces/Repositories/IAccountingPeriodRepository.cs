using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IAccountingPeriodRepository
{
    Task<bool>                   IsClosedAsync(Guid orgId, int year, int month, CancellationToken ct = default);
    Task<List<AccountingPeriod>> GetClosedForYearAsync(Guid orgId, int year, CancellationToken ct = default);
    Task<AccountingPeriod?>      GetAsync(Guid orgId, int year, int month, CancellationToken ct = default);
    Task                         AddAsync(AccountingPeriod period, CancellationToken ct = default);
    void                         Remove(AccountingPeriod period);
    Task                         SaveChangesAsync(CancellationToken ct = default);
}
