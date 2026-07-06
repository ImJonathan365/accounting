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
                     && !l.JournalEntry.IsYearEndClosing
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

    public async Task<(List<LedgerLineData> Items, int Total)> GetLedgerLinesPagedAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.AccountId == accountId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date >= from
                     && l.JournalEntry.Date <= to)
            .OrderBy(l => l.JournalEntry.Date)
            .ThenBy(l => l.JournalEntry.CreatedAtUtc);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LedgerLineData(
                l.JournalEntry.Id,
                l.JournalEntry.Date,
                l.JournalEntry.Description,
                l.JournalEntry.Reference,
                l.Debit,
                l.Credit))
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<List<MonthlyLineData>> GetMonthlyTrendsAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && !l.JournalEntry.IsYearEndClosing
                     && l.JournalEntry.Date >= from
                     && l.JournalEntry.Date <= to)
            .GroupBy(l => new { l.JournalEntry.Date.Year, l.JournalEntry.Date.Month, l.AccountId })
            .Select(g => new MonthlyLineData(
                g.Key.Year, g.Key.Month, g.Key.AccountId,
                g.Sum(l => l.Debit), g.Sum(l => l.Credit)))
            .ToListAsync(ct);

    public Task<decimal> GetLedgerBalanceInRangeAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to,
        int skip, CancellationToken ct = default) =>
        _db.JournalLines
            .Where(l => l.JournalEntry.OrganizationId == orgId
                     && l.AccountId == accountId
                     && l.JournalEntry.Status == JournalStatus.Posted
                     && l.JournalEntry.Date >= from
                     && l.JournalEntry.Date <= to)
            .OrderBy(l => l.JournalEntry.Date)
            .ThenBy(l => l.JournalEntry.CreatedAtUtc)
            .Take(skip)
            .SumAsync(l => l.Debit - l.Credit, ct);

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
