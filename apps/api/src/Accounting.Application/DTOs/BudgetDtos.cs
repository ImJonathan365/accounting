namespace Accounting.Application.DTOs;

public record BudgetLineDto(
    Guid    Id,
    Guid    AccountId,
    string  AccountCode,
    string  AccountName,
    string  AccountType,
    int     Month,
    decimal Amount);

public record BudgetDto(
    Guid   Id,
    int    Year,
    string Name,
    bool   IsActive,
    List<BudgetLineDto> Lines);

public record CreateBudgetDto(string Name, int Year);

public record UpsertBudgetLineDto(Guid AccountId, int Month, decimal Amount);

public record BudgetVsActualLineDto(
    Guid    AccountId,
    string  AccountCode,
    string  AccountName,
    string  AccountType,
    decimal[] Budget,
    decimal[] Actual,
    decimal   TotalBudget,
    decimal   TotalActual,
    decimal   Variance);

public record BudgetVsActualDto(
    int    Year,
    string BudgetName,
    List<BudgetVsActualLineDto> Lines);
