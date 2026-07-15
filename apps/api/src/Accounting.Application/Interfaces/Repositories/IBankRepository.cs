using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IBankRepository
{
    Task<List<BankAccount>>       GetAccountsAsync(Guid orgId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>>   GetPendingCountsAsync(Guid orgId, CancellationToken ct = default);
    Task<BankAccount?>            GetAccountAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task                       AddAccountAsync(BankAccount account, CancellationToken ct = default);
    Task<List<BankTransaction>> GetTransactionsAsync(Guid bankAccountId, CancellationToken ct = default);
    Task<(List<BankTransaction> Items, int Total)> GetPendingTransactionsPagedAsync(Guid bankAccountId, int page, int pageSize, CancellationToken ct = default);
    Task<decimal>          GetBankBalanceAsync(Guid bankAccountId, CancellationToken ct = default);
    Task<decimal>          GetGLBalanceAsync(Guid orgId, Guid linkedAccountId, CancellationToken ct = default);
    Task<BankTransaction?> GetTransactionAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task                        AddTransactionAsync(BankTransaction tx, CancellationToken ct = default);
    Task                        AddTransactionsAsync(IEnumerable<BankTransaction> txs, CancellationToken ct = default);
    Task<List<UnmatchedJournalEntry>> GetUnmatchedJournalEntriesAsync(Guid orgId, Guid linkedAccountId, CancellationToken ct = default);
    Task                        SaveChangesAsync(CancellationToken ct = default);
}

public record UnmatchedJournalEntry(Guid Id, DateOnly Date, string Reference, string Description, decimal Amount);
