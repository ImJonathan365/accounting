namespace Accounting.Application.DTOs;

public record OpeningBalanceLineRequest(Guid AccountId, decimal Debit, decimal Credit);

public record SetOpeningBalancesRequest(
    string Date,
    string? Description,
    List<OpeningBalanceLineRequest> Lines);

public record OpeningBalanceResultDto(Guid JournalEntryId, string Date, decimal TotalDebit, decimal TotalCredit);
