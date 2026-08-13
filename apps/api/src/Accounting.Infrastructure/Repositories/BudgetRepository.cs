using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _db;
    public BudgetRepository(AppDbContext db) => _db = db;

    public Task<List<Budget>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Budgets.AsNoTracking()
            .Include(b => b.Lines).ThenInclude(l => l.Account)
            .Where(b => b.OrganizationId == orgId)
            .OrderByDescending(b => b.Year).ThenBy(b => b.Name)
            .ToListAsync(ct);

    public Task<Budget?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Budgets
            .Include(b => b.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(b => b.OrganizationId == orgId && b.Id == id, ct);

    public async Task AddAsync(Budget budget, CancellationToken ct = default) =>
        await _db.Budgets.AddAsync(budget, ct);

    public void Remove(Budget budget) => _db.Budgets.Remove(budget);

    public void RemoveLine(BudgetLine line) => _db.BudgetLines.Remove(line);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
