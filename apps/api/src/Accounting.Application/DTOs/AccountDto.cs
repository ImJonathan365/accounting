using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record AccountDto(
    Guid Id, string Code, string Name, AccountType Type,
    Guid? ParentId, bool IsPostable, bool IsActive);

public record CreateAccountDto(
    string Code, string Name, AccountType Type, Guid? ParentId, bool IsPostable = true);
