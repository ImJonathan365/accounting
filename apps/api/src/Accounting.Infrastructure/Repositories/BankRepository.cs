using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class BankRepository : IBankRepository
{
    private readonly AppDbContext _db;
    public BankRepository(AppDbContext db) => _db = db;

    public Task<List<BankAccount>> GetAccountsAsync(Guid orgId, CancellationToken ct = default) =>
        _db.BankAccounts.AsNoTracking()
            .Include(a => a.LinkedAccount)
            .Where(a => a.OrganizationId == orgId)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, int>> GetPendingCountsAsync(Guid orgId, CancellationToken ct = default) =>
        await _db.BankTransactions
            .Where(t => t.BankAccount.OrganizationId == orgId && t.Status == BankTransactionStatus.Pending)
            .GroupBy(t => t.BankAccountId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public Task<BankAccount?> GetAccountAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.BankAccounts
            .Include(a => a.LinkedAccount)
            .FirstOrDefaultAsync(a => a.OrganizationId == orgId && a.Id == id, ct);

    public async Task AddAccountAsync(BankAccount account, CancellationToken ct = default) =>
        await _db.BankAccounts.AddAsync(account, ct);

    public Task<List<BankTransaction>> GetTransactionsAsync(Guid bankAccountId, CancellationToken ct = default) =>
        _db.BankTransactions.AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

    public async Task<(List<BankTransaction> Items, int Total)> GetPendingTransactionsPagedAsync(
        Guid bankAccountId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.BankTransactions.AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId && t.Status == BankTransactionStatus.Pending)
            .OrderByDescending(t => t.Date);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<decimal> GetBankBalanceAsync(Guid bankAccountId, CancellationToken ct = default) =>
        _db.BankTransactions.AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId && t.Status != BankTransactionStatus.Excluded)
            .SumAsync(t => t.Type == BankTransactionType.Credit ? t.Amount : -t.Amount, ct);

    public Task<decimal> GetGLBalanceAsync(Guid orgId, Guid linkedAccountId, CancellationToken ct = default) =>
        _db.JournalLines.AsNoTracking()
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.AccountId == linkedAccountId
                     && l.JournalEntry.Status == JournalStatus.Posted)
            .SumAsync(l => l.Debit - l.Credit, ct);

    public Task<BankTransaction?> GetTransactionAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.BankTransactions.FirstOrDefaultAsync(
            t => t.Id == id && t.BankAccount.OrganizationId == orgId, ct);

    public async Task AddTransactionAsync(BankTransaction tx, CancellationToken ct = default) =>
        await _db.BankTransactions.AddAsync(tx, ct);

    public async Task AddTransactionsAsync(IEnumerable<BankTransaction> txs, CancellationToken ct = default) =>
        await _db.BankTransactions.AddRangeAsync(txs, ct);

    public Task<List<UnmatchedJournalEntry>> GetUnmatchedJournalEntriesAsync(
        Guid orgId, Guid linkedAccountId, CancellationToken ct = default)
    {
        var matchedEntryIds = _db.BankTransactions
            .Where(t => t.JournalEntryId != null)
            .Select(t => t.JournalEntryId!.Value);

        return _db.JournalEntries
            .Where(e => e.OrganizationId == orgId
                     && e.Status == JournalStatus.Posted
                     && !e.IsYearEndClosing
                     && !matchedEntryIds.Contains(e.Id)
                     && e.Lines.Any(l => l.AccountId == linkedAccountId))
            .OrderByDescending(e => e.Date)
            .Select(e => new UnmatchedJournalEntry(
                e.Id, e.Date, e.Reference ?? "",
                e.Description,
                e.Lines.Where(l => l.AccountId == linkedAccountId).Sum(l => l.Debit - l.Credit)))
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
