using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class JournalRepository : IJournalRepository
{
    private readonly AppDbContext _db;
    public JournalRepository(AppDbContext db) => _db = db;

    public Task<List<JournalEntry>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.JournalEntries.AsNoTracking()
            .Where(e => e.OrganizationId == orgId)
            .Include(e => e.Lines)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<JournalEntry?> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default) =>
        _db.JournalEntries.AsNoTracking()
            .Where(e => e.Id == id && e.OrganizationId == orgId)
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(ct);

    public Task<JournalEntry?> GetByIdTrackedAsync(Guid id, Guid orgId, CancellationToken ct = default) =>
        _db.JournalEntries
            .Where(e => e.Id == id && e.OrganizationId == orgId)
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default) =>
        await _db.JournalEntries.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
