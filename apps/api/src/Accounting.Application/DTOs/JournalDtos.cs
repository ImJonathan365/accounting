using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record CreateJournalLineDto(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? Note);

public record CreateJournalEntryDto(
    DateOnly Date,
    string Description,
    string? Reference,
    List<CreateJournalLineDto> Lines);

public record JournalLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Note);

public record JournalEntryDto(
    Guid Id,
    DateOnly Date,
    string Description,
    string? Reference,
    JournalStatus Status,
    decimal TotalDebit,
    decimal TotalCredit,
    List<JournalLineDto> Lines,
    DateTime CreatedAtUtc,
    Guid? VoidsEntryId,
    Guid? VoidedByEntryId,
    string? VoidReason,
    DateTime? VoidedAtUtc);

public record JournalEntrySummaryDto(
    Guid Id,
    DateOnly Date,
    string Description,
    string? Reference,
    JournalStatus Status,
    decimal TotalDebit,
    DateTime CreatedAtUtc,
    Guid? VoidsEntryId,
    Guid? VoidedByEntryId);

public record VoidJournalEntryDto(
    string? Reason,
    DateOnly? VoidDate);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
