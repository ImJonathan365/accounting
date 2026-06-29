using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Organization org, CancellationToken ct = default);
    Task AddMembershipAsync(Membership membership, CancellationToken ct = default);
    Task<Membership?> GetFirstMembershipAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
