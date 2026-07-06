using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class YearEndClosingRepository : IYearEndClosingRepository
{
    private readonly AppDbContext _db;
    public YearEndClosingRepository(AppDbContext db) => _db = db;

    public Task<YearEndClosing?> GetAsync(Guid orgId, int year, CancellationToken ct = default) =>
        _db.YearEndClosings
            .Include(y => y.ClosedBy)
            .FirstOrDefaultAsync(y => y.OrganizationId == orgId && y.Year == year, ct);

    public async Task AddAsync(YearEndClosing closing, CancellationToken ct = default) =>
        await _db.YearEndClosings.AddAsync(closing, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
