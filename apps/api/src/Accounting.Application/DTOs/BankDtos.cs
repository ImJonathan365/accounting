using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record BankAccountDto(
    Guid   Id,
    string Name,
    string? BankName,
    string? AccountNumber,
    Guid   LinkedAccountId,
    string LinkedAccountCode,
    string LinkedAccountName,
    bool   IsActive,
    int    PendingCount);

public record CreateBankAccountDto(
    string Name,
    string? BankName,
    string? AccountNumber,
    Guid   LinkedAccountId);

public record BankTransactionDto(
    Guid                  Id,
    Guid                  BankAccountId,
    string                Date,
    string                Description,
    decimal               Amount,
    BankTransactionType   Type,
    BankTransactionStatus Status,
    Guid?                 JournalEntryId,
    string?               Notes);

public record ImportBankTransactionDto(
    string  Date,
    string  Description,
    decimal Amount,
    string  Type);

public record MatchTransactionDto(Guid JournalEntryId);

public record BankReconciliationDto(
    BankAccountDto             BankAccount,
    List<BankTransactionDto>   UnmatchedTransactions,
    List<UnmatchedJournalDto>  UnmatchedJournalEntries,
    decimal                    BankBalance,
    decimal                    BookBalance,
    decimal                    Difference);

public record UnmatchedJournalDto(
    Guid    JournalEntryId,
    string  Date,
    string  Reference,
    string  Description,
    decimal Amount);
