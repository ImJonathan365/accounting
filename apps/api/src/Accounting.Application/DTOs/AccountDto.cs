using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record AccountDto(
    Guid Id, string Code, string Name, AccountType Type,
    Guid? ParentId, bool IsPostable, bool IsActive, CashFlowSection CashFlowSection);

public record CreateAccountDto(
    string Code, string Name, AccountType Type, Guid? ParentId,
    bool IsPostable = true, CashFlowSection CashFlowSection = CashFlowSection.None);

public record UpdateAccountDto(string Name, bool IsPostable, CashFlowSection CashFlowSection = CashFlowSection.None);
