using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;
    public ReportRepository(AppDbContext db) => _db = db;

    public Task<List<AccountBalanceData>> GetAccountBalancesAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date >= from
                     && l.JournalEntry.Date <= to)
            .GroupBy(l => l.AccountId)
            .Select(g => new AccountBalanceData(g.Key, g.Sum(l => l.Debit), g.Sum(l => l.Credit)))
            .ToListAsync(ct);

    public Task<List<AccountBalanceData>> GetCumulativeBalancesAsync(
        Guid orgId, DateOnly asOf, CancellationToken ct = default) =>
        _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new AccountBalanceData(g.Key, g.Sum(l => l.Debit), g.Sum(l => l.Credit)))
            .ToListAsync(ct);

    public Task<List<LedgerLineData>> GetLedgerLinesAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.AccountId == accountId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date >= from
                     && l.JournalEntry.Date <= to)
            .OrderBy(l => l.JournalEntry.Date)
            .ThenBy(l => l.JournalEntry.CreatedAtUtc)
            .Select(l => new LedgerLineData(
                l.JournalEntry.Id,
                l.JournalEntry.Date,
                l.JournalEntry.Description,
                l.JournalEntry.Reference,
                l.Debit,
                l.Credit))
            .ToListAsync(ct);

    public async Task<decimal> GetAccountOpeningBalanceAsync(
        Guid orgId, Guid accountId, DateOnly before, CancellationToken ct = default)
    {
        var q = _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.AccountId == accountId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date < before);

        var debit  = await q.SumAsync(l => l.Debit,  ct);
        var credit = await q.SumAsync(l => l.Credit, ct);
        return debit - credit;
    }
}
