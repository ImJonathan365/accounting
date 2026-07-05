using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Organization org, CancellationToken ct = default);
    Task AddMembershipAsync(Membership membership, CancellationToken ct = default);
    Task<Membership?> GetFirstMembershipAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsOrgMemberAsync(Guid orgId, Guid userId, CancellationToken ct = default);
    Task<List<Membership>> GetAllMembershipsForUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<Membership>> GetMembersWithUsersAsync(Guid orgId, CancellationToken ct = default);
    Task<Membership?> GetMemberTrackedAsync(Guid orgId, Guid userId, CancellationToken ct = default);
    Task<string?> GetMemberRoleAsync(Guid orgId, Guid userId, CancellationToken ct = default);
    void RemoveMembership(Membership membership);
    Task SaveChangesAsync(CancellationToken ct = default);
}
