using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class RecurringEntryRepository : IRecurringEntryRepository
{
    private readonly AppDbContext _db;
    public RecurringEntryRepository(AppDbContext db) => _db = db;

    public Task<List<RecurringJournalEntry>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.RecurringJournalEntries
            .Where(r => r.OrganizationId == orgId)
            .Include(r => r.Lines).ThenInclude(l => l.Account)
            .OrderBy(r => r.NextDate)
            .ToListAsync(ct);

    public Task<RecurringJournalEntry?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.RecurringJournalEntries
            .Where(r => r.OrganizationId == orgId && r.Id == id)
            .Include(r => r.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(ct);

    public Task<List<RecurringJournalEntry>> GetPendingAsync(Guid orgId, DateOnly asOf, CancellationToken ct = default) =>
        _db.RecurringJournalEntries
            .Where(r => r.OrganizationId == orgId && r.IsActive && r.NextDate <= asOf
                     && (r.EndDate == null || r.NextDate <= r.EndDate))
            .Include(r => r.Lines).ThenInclude(l => l.Account)
            .ToListAsync(ct);

    public async Task AddAsync(RecurringJournalEntry entry, CancellationToken ct = default) =>
        await _db.RecurringJournalEntries.AddAsync(entry, ct);

    public void Remove(RecurringJournalEntry entry) =>
        _db.RecurringJournalEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
