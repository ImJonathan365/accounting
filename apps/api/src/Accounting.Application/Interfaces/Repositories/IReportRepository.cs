using Accounting.Application.DTOs;

namespace Accounting.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<List<AccountBalanceData>> GetAccountBalancesAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);

    // Cumulative from the beginning of records up to asOf (for balance sheet)
    Task<List<AccountBalanceData>> GetCumulativeBalancesAsync(
        Guid orgId, DateOnly asOf, CancellationToken ct = default);
}
