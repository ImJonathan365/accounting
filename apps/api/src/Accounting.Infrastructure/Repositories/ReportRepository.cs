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
}
