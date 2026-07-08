using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class TaxRateRepository : ITaxRateRepository
{
    private readonly AppDbContext _db;
    public TaxRateRepository(AppDbContext db) => _db = db;

    public Task<List<TaxRate>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.TaxRates.AsNoTracking()
            .Include(t => t.TaxAccount)
            .Where(t => t.OrganizationId == orgId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public Task<TaxRate?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.TaxRates
            .Include(t => t.TaxAccount)
            .FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.Id == id, ct);

    public Task<List<TaxRate>> GetByIdsAsync(IEnumerable<Guid> ids, Guid orgId, CancellationToken ct = default) =>
        _db.TaxRates.AsNoTracking()
            .Where(t => t.OrganizationId == orgId && ids.Contains(t.Id))
            .ToListAsync(ct);

    public async Task AddAsync(TaxRate taxRate, CancellationToken ct = default) =>
        await _db.TaxRates.AddAsync(taxRate, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
