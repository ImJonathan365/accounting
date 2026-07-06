using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record RecurringLineDto(
    Guid    Id,
    Guid    AccountId,
    string  AccountCode,
    string  AccountName,
    decimal Debit,
    decimal Credit,
    string? Note);

public record RecurringEntryDto(
    Guid                  Id,
    string                Description,
    string?               Reference,
    RecurringFrequency    Frequency,
    string                FrequencyLabel,
    DateOnly              NextDate,
    DateOnly?             EndDate,
    bool                  IsActive,
    List<RecurringLineDto> Lines);

public record CreateRecurringLineDto(
    Guid    AccountId,
    decimal Debit,
    decimal Credit,
    string? Note = null);

public record CreateRecurringEntryDto(
    string                      Description,
    string?                     Reference,
    RecurringFrequency          Frequency,
    DateOnly                    StartDate,
    DateOnly?                   EndDate,
    List<CreateRecurringLineDto> Lines);

public record UpdateRecurringEntryDto(
    string?            Description,
    string?            Reference,
    RecurringFrequency? Frequency,
    DateOnly?           NextDate,
    DateOnly?           EndDate,
    bool?               IsActive);

public record GeneratePendingResultDto(int Generated, List<Guid> EntryIds);
