using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class AccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly AppDbContext _db;
    public AccountingPeriodRepository(AppDbContext db) => _db = db;

    public Task<bool> IsClosedAsync(Guid orgId, int year, int month, CancellationToken ct = default) =>
        _db.AccountingPeriods.AnyAsync(
            p => p.OrganizationId == orgId && p.Year == year && p.Month == month, ct);

    public Task<List<AccountingPeriod>> GetClosedForYearAsync(Guid orgId, int year, CancellationToken ct = default) =>
        _db.AccountingPeriods
            .Include(p => p.ClosedBy)
            .Where(p => p.OrganizationId == orgId && p.Year == year)
            .ToListAsync(ct);

    public Task<AccountingPeriod?> GetAsync(Guid orgId, int year, int month, CancellationToken ct = default) =>
        _db.AccountingPeriods
            .Include(p => p.ClosedBy)
            .FirstOrDefaultAsync(
                p => p.OrganizationId == orgId && p.Year == year && p.Month == month, ct);

    public async Task AddAsync(AccountingPeriod period, CancellationToken ct = default) =>
        await _db.AccountingPeriods.AddAsync(period, ct);

    public void Remove(AccountingPeriod period) =>
        _db.AccountingPeriods.Remove(period);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
