using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record RecentEntryDto(
    Guid Id,
    DateOnly Date,
    string Description,
    string? Reference,
    decimal TotalDebit,
    JournalStatus Status);

public record DashboardSummaryDto(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal NetIncome,
    bool IsProfit,
    bool IsBalanced,
    List<RecentEntryDto> RecentEntries,
    string CurrencySymbol,
    string PeriodLabel);
