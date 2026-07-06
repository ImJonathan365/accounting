namespace Accounting.Application.DTOs;

public record YearEndStatusDto(
    int       Year,
    bool      IsClosed,
    string?   ClosedByName,
    DateTime? ClosedAtUtc,
    Guid?     JournalEntryId);

public record YearEndCloseRequestDto(int Year, Guid RetainedEarningsAccountId);
