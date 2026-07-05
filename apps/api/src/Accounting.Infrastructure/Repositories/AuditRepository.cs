using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;
    public AuditRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await _db.AuditLogs.AddAsync(log, ct);

    public async Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        Guid orgId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.OrganizationId == orgId)
            .OrderByDescending(a => a.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
