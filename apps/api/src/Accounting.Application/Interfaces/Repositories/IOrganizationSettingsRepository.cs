using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IOrganizationSettingsRepository
{
    Task<OrganizationSettings?> GetByOrgIdAsync(Guid orgId, CancellationToken ct = default);
    Task AddAsync(OrganizationSettings settings, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
