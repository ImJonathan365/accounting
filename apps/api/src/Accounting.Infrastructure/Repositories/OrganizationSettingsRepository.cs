using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class OrganizationSettingsRepository : IOrganizationSettingsRepository
{
    private readonly AppDbContext _db;
    public OrganizationSettingsRepository(AppDbContext db) => _db = db;

    public Task<OrganizationSettings?> GetByOrgIdAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Set<OrganizationSettings>().FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

    public async Task AddAsync(OrganizationSettings settings, CancellationToken ct = default) =>
        await _db.Set<OrganizationSettings>().AddAsync(settings, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
