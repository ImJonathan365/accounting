using Accounting.Application.DTOs;

namespace Accounting.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<List<AccountBalanceData>> GetAccountBalancesAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<List<AccountBalanceData>> GetCumulativeBalancesAsync(
        Guid orgId, DateOnly asOf, CancellationToken ct = default);

    Task<(List<LedgerLineData> Items, int Total)> GetLedgerLinesPagedAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to,
        int page, int pageSize, CancellationToken ct = default);

    Task<decimal> GetLedgerBalanceInRangeAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to,
        int skip, CancellationToken ct = default);

    Task<decimal> GetAccountOpeningBalanceAsync(
        Guid orgId, Guid accountId, DateOnly before, CancellationToken ct = default);

    Task<List<MonthlyLineData>> GetMonthlyTrendsAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
