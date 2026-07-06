namespace Accounting.Application.DTOs;

public record PeriodDto(
    int       Year,
    int       Month,
    string    MonthName,
    bool      IsClosed,
    string?   ClosedByName,
    DateTime? ClosedAtUtc);

public record ClosePeriodDto(int Year, int Month);
