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
        _db.Memberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Role == "owner" ? 0 : 1)
            .ThenBy(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    public Task<bool> IsOrgMemberAsync(Guid orgId, Guid userId, CancellationToken ct = default) =>
        _db.Memberships.AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

    public Task<List<Membership>> GetAllMembershipsForUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.Memberships.AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<List<Membership>> GetMembersWithUsersAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Memberships.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == orgId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<Membership?> GetMemberTrackedAsync(Guid orgId, Guid userId, CancellationToken ct = default) =>
        _db.Memberships.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

    public Task<string?> GetMemberRoleAsync(Guid orgId, Guid userId, CancellationToken ct = default) =>
        _db.Memberships
            .Where(m => m.OrganizationId == orgId && m.UserId == userId)
            .Select(m => (string?)m.Role)
            .FirstOrDefaultAsync(ct);

    public void RemoveMembership(Membership membership) => _db.Memberships.Remove(membership);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
