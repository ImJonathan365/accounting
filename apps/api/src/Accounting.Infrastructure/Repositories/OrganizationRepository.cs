using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;
    public OrganizationRepository(AppDbContext db) => _db = db;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Organization org, CancellationToken ct = default) =>
        await _db.Organizations.AddAsync(org, ct);

    public async Task AddMembershipAsync(Membership membership, CancellationToken ct = default) =>
        await _db.Memberships.AddAsync(membership, ct);

    public Task<Membership?> GetFirstMembershipAsync(Guid userId, CancellationToken ct = default) =>
        _db.Memberships.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
