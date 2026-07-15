using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
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

    public async Task<(List<JournalEntry> Items, int Total)> GetPagedAsync(
        Guid orgId, int page, int pageSize,
        DateOnly? from = null, DateOnly? to = null,
        JournalStatus? status = null, string? search = null,
        CancellationToken ct = default)
    {
        var q = _db.JournalEntries.AsNoTracking()
            .Where(e => e.OrganizationId == orgId);

        if (from.HasValue)   q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue)     q = q.Where(e => e.Date <= to.Value);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = search.Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escapedSearch}%";
            q = q.Where(e => EF.Functions.ILike(e.Description, pattern, "\\")
                          || (e.Reference != null && EF.Functions.ILike(e.Reference, pattern, "\\")));
        }

        q = q.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.Lines)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

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

    public Task<List<JournalEntry>> GetRecentAsync(Guid orgId, int count, CancellationToken ct = default) =>
        _db.JournalEntries.AsNoTracking()
            .Where(e => e.OrganizationId == orgId)
            .Include(e => e.Lines)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Take(count)
            .ToListAsync(ct);

    public Task<bool> ExistsWithReferenceAsync(Guid orgId, string reference, CancellationToken ct = default) =>
        _db.JournalEntries.AnyAsync(
            e => e.OrganizationId == orgId && e.Reference == reference && e.Status != JournalStatus.Voided, ct);

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default) =>
        await _db.JournalEntries.AddAsync(entry, ct);

    public void Delete(JournalEntry entry) => _db.JournalEntries.Remove(entry);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
