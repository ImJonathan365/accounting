using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IJournalRepository
{
    Task<List<JournalEntry>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<JournalEntry?> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default);

    /// <summary>Same as GetByIdAsync but with EF change-tracking enabled (for mutations).</summary>
    Task<JournalEntry?> GetByIdTrackedAsync(Guid id, Guid orgId, CancellationToken ct = default);

    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
