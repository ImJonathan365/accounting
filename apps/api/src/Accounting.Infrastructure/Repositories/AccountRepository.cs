using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;

    public Task<List<Account>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Accounts.AsNoTracking().Where(a => a.OrganizationId == orgId)
            .OrderBy(a => a.Code).ToListAsync(ct);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Account?> GetByIdTrackedAsync(Guid id, Guid orgId, CancellationToken ct = default) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == orgId, ct);

    public Task<List<Account>> GetByIdsAsync(Guid orgId, IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return _db.Accounts.AsNoTracking()
            .Where(a => a.OrganizationId == orgId && idList.Contains(a.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default) =>
        await _db.Accounts.AddAsync(account, ct);

    public async Task AddRangeAsync(IEnumerable<Account> accounts, CancellationToken ct = default) =>
        await _db.Accounts.AddRangeAsync(accounts, ct);

    public Task<bool> CodeExistsAsync(Guid orgId, string code, CancellationToken ct = default) =>
        _db.Accounts.AnyAsync(a => a.OrganizationId == orgId && a.Code == code, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
