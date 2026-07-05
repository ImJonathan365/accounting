using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        Guid orgId, int page, int pageSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
