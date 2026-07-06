using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IYearEndClosingRepository
{
    Task<YearEndClosing?> GetAsync(Guid orgId, int year, CancellationToken ct = default);
    Task AddAsync(YearEndClosing closing, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
