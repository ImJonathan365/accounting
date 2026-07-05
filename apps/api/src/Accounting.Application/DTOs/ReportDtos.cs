using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record AccountBalanceData(Guid AccountId, decimal TotalDebit, decimal TotalCredit);

public record LedgerLineData(
    Guid     EntryId,
    DateOnly Date,
    string   Description,
    string?  Reference,
    decimal  Debit,
    decimal  Credit);

// ── Ledger ────────────────────────────────────────────────────────────────────

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
    decimal     ClosingBalance);

// ── Trial Balance ─────────────────────────────────────────────────────────────

public record TrialBalanceLineDto(
    Guid   AccountId,
    string Code,
    string Name,
    AccountType Type,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal DebitBalance,    // net saldo deudor  (debit > credit)
    decimal CreditBalance);  // net saldo acreedor (credit > debit)

public record TrialBalanceDto(
    DateOnly From,
    DateOnly To,
    List<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalDebitBalance,
    decimal TotalCreditBalance,
    bool IsBalanced);

// ── Balance Sheet ─────────────────────────────────────────────────────────────

public record BalanceSheetLineDto(
    Guid    AccountId,
    string  Code,
    string  Name,
    decimal Balance);         // always expressed as positive contribution to its section

public record BalanceSheetSectionDto(
    string  SectionCode,      // parent account code  (e.g. "1.1")
    string  SectionName,      // parent account name  (e.g. "Activo Corriente")
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
    BalanceSheetGroupDto Equity,          // equity account balances only
    decimal NetIncome,                    // income – expenses, shown as "Utilidad acumulada"
    decimal TotalEquity,                  // Equity.Total + NetIncome
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced);

// ── Income Statement ──────────────────────────────────────────────────────────

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
