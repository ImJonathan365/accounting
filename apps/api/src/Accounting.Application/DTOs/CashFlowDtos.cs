namespace Accounting.Application.DTOs;

public record CashFlowLineDto(string AccountCode, string AccountName, decimal Amount);

public record CashFlowSectionDto(
    string                  Title,
    List<CashFlowLineDto>   Lines,
    decimal                 NetIncome,    // only populated for Operating
    decimal                 Total);

public record CashFlowDto(
    DateOnly             From,
    DateOnly             To,
    decimal              BeginningCash,
    CashFlowSectionDto   Operating,
    CashFlowSectionDto   Investing,
    CashFlowSectionDto   Financing,
    decimal              NetChange,
    decimal              EndingCash);
