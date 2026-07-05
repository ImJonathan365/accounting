namespace Accounting.Application.DTOs;

public record AuditLogDto(
    Guid     Id,
    Guid     UserId,
    string   UserName,
    string   Action,
    string   EntityType,
    Guid     EntityId,
    string?  Details,
    DateTime CreatedAtUtc);
