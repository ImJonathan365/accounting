using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record AccountBalanceData(Guid AccountId, decimal TotalDebit, decimal TotalCredit);

public record MonthlyLineData(int Year, int Month, Guid AccountId, decimal Debit, decimal Credit);

public record LedgerLineData(
    Guid     EntryId,
    DateOnly Date,
    string   Description,
    string?  Reference,
    decimal  Debit,
    decimal  Credit);

public record LedgerLineDto(
    Guid     EntryId,
    DateOnly Date,
    string   Description,
    string?  Reference,
    decimal  Debit,
    decimal  Credit,
    decimal  RunningBalance);

public record LedgerDto(
    Guid        AccountId,
    string      AccountCode,
    string      AccountName,
    AccountType AccountType,
    DateOnly    From,
    DateOnly    To,
    decimal     OpeningBalance,
    IReadOnlyList<LedgerLineDto> Lines,
    decimal     ClosingBalance,
    int         TotalLines,
    int         Page,
    int         PageSize,
    int         TotalPages);

public record TrialBalanceLineDto(
    Guid   AccountId,
    string Code,
    string Name,
    AccountType Type,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal DebitBalance,
    decimal CreditBalance);

public record TrialBalanceDto(
    DateOnly From,
    DateOnly To,
    List<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalDebitBalance,
    decimal TotalCreditBalance,
    bool IsBalanced);

public record BalanceSheetLineDto(
    Guid    AccountId,
    string  Code,
    string  Name,
    decimal Balance);

public record BalanceSheetSectionDto(
    string  SectionCode,
    string  SectionName,
    List<BalanceSheetLineDto> Lines,
    decimal Subtotal);

public record BalanceSheetGroupDto(
    string  Title,
    List<BalanceSheetSectionDto> Sections,
    decimal Total);

public record BalanceSheetDto(
    DateOnly AsOf,
    BalanceSheetGroupDto Assets,
    BalanceSheetGroupDto Liabilities,
    BalanceSheetGroupDto Equity,
    decimal NetIncome,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced);

public record IncomeStatementLineDto(
    Guid    AccountId,
    string  Code,
    string  Name,
    string? ParentCode,
    string? ParentName,
    decimal Amount);

public record IncomeStatementSectionDto(
    string Title,
    List<IncomeStatementLineDto> Lines,
    decimal Total);

public record IncomeStatementDto(
    DateOnly From,
    DateOnly To,
    IncomeStatementSectionDto Income,
    IncomeStatementSectionDto Expenses,
    decimal NetIncome,
    bool IsProfit);
