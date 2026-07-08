using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record RecentEntryDto(
    Guid Id,
    DateOnly Date,
    string Description,
    string? Reference,
    decimal TotalDebit,
    JournalStatus Status);

public record MonthlyTrendDto(int Year, int Month, string Label, decimal Income, decimal Expense);

public record FinancialRatiosDto(
    decimal? LiquidityRatio,
    decimal  DebtRatio,
    decimal? NetProfitMargin);

public record OverdueInvoiceDto(
    Guid   Id,
    string Number,
    string ContactName,
    string DueDate,
    string Type,
    decimal Balance);

public record DashboardSummaryDto(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal NetIncome,
    bool IsProfit,
    bool IsBalanced,
    List<RecentEntryDto> RecentEntries,
    string CurrencySymbol,
    string PeriodLabel,
    List<MonthlyTrendDto> MonthlyTrend,
    FinancialRatiosDto Ratios,
    // Invoice KPIs
    int     OverdueCount,
    decimal OverdueAmount,
    decimal PendingReceivable,
    decimal PendingPayable,
    List<OverdueInvoiceDto> OverdueInvoices);
