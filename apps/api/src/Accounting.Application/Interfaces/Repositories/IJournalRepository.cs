using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IJournalRepository
{
    Task<List<JournalEntry>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<(List<JournalEntry> Items, int Total)> GetPagedAsync(
        Guid orgId, int page, int pageSize,
        DateOnly? from = null, DateOnly? to = null,
        JournalStatus? status = null, string? search = null,
        CancellationToken ct = default);
    Task<JournalEntry?> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default);

    /// <summary>Same as GetByIdAsync but with EF change-tracking enabled (for mutations).</summary>
    Task<JournalEntry?> GetByIdTrackedAsync(Guid id, Guid orgId, CancellationToken ct = default);

    Task<List<JournalEntry>> GetRecentAsync(Guid orgId, int count, CancellationToken ct = default);

    Task<bool> ExistsWithReferenceAsync(Guid orgId, string reference, CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    void Delete(JournalEntry entry);
    Task SaveChangesAsync(CancellationToken ct = default);
}
