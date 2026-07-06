using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IRecurringEntryRepository
{
    Task<List<RecurringJournalEntry>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<RecurringJournalEntry?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<List<RecurringJournalEntry>> GetPendingAsync(Guid orgId, DateOnly asOf, CancellationToken ct = default);
    Task AddAsync(RecurringJournalEntry entry, CancellationToken ct = default);
    void Remove(RecurringJournalEntry entry);
    Task SaveChangesAsync(CancellationToken ct = default);
}
