using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IRecurringEntryService
{
    Task<List<RecurringEntryDto>> GetAllAsync(Guid orgId, CancellationToken ct = default);
    Task<RecurringEntryDto>       GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<RecurringEntryDto>       CreateAsync(Guid orgId, CreateRecurringEntryDto dto, CancellationToken ct = default);
    Task<RecurringEntryDto>       UpdateAsync(Guid orgId, Guid id, UpdateRecurringEntryDto dto, CancellationToken ct = default);
    Task                          DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<GeneratePendingResultDto> GeneratePendingAsync(Guid orgId, CancellationToken ct = default);
}

public class RecurringEntryService : IRecurringEntryService
{
    private readonly IRecurringEntryRepository _repo;
    private readonly IAccountRepository        _accounts;
    private readonly IJournalRepository        _journal;

    public RecurringEntryService(
        IRecurringEntryRepository repo,
        IAccountRepository accounts,
        IJournalRepository journal)
    {
        _repo     = repo;
        _accounts = accounts;
        _journal  = journal;
    }

    public async Task<List<RecurringEntryDto>> GetAllAsync(Guid orgId, CancellationToken ct = default)
    {
        var entries = await _repo.GetByOrganizationAsync(orgId, ct);
        return entries.Select(Map).ToList();
    }

    public async Task<RecurringEntryDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entry = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Asiento recurrente no encontrado.");
        return Map(entry);
    }

    public async Task<RecurringEntryDto> CreateAsync(Guid orgId, CreateRecurringEntryDto dto, CancellationToken ct = default)
    {
        if (!dto.Lines.Any())
            throw new ArgumentException("Debe incluir al menos una línea.");

        var totalDebit  = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
            throw new ArgumentException("El total de débitos debe ser igual al total de créditos.");

        var lineAccountIds = dto.Lines.Select(l => l.AccountId).Distinct();
        var foundAccounts  = await _accounts.GetByIdsAsync(orgId, lineAccountIds, ct);
        var validIds       = foundAccounts.Where(a => a.IsPostable).Select(a => a.Id).ToHashSet();

        var entry = new RecurringJournalEntry
        {
            OrganizationId = orgId,
            Description    = dto.Description.Trim(),
            Reference      = dto.Reference?.Trim(),
            Frequency      = dto.Frequency,
            NextDate       = dto.StartDate,
            EndDate        = dto.EndDate,
            Lines          = dto.Lines.Select(l =>
            {
                if (!validIds.Contains(l.AccountId))
                    throw new ArgumentException($"Cuenta {l.AccountId} no encontrada o no es postable.");
                return new RecurringJournalLine
                {
                    AccountId = l.AccountId,
                    Debit     = l.Debit,
                    Credit    = l.Credit,
                    Note      = l.Note,
                };
            }).ToList(),
        };

        await _repo.AddAsync(entry, ct);
        await _repo.SaveChangesAsync(ct);

        var saved = await _repo.GetByIdAsync(orgId, entry.Id, ct);
        return Map(saved!);
    }

    public async Task<RecurringEntryDto> UpdateAsync(Guid orgId, Guid id, UpdateRecurringEntryDto dto, CancellationToken ct = default)
    {
        var entry = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Asiento recurrente no encontrado.");

        if (dto.Description is not null) entry.Description = dto.Description.Trim();
        if (dto.Reference   is not null) entry.Reference   = dto.Reference.Trim();
        if (dto.Frequency   is not null) entry.Frequency   = dto.Frequency.Value;
        if (dto.NextDate    is not null) entry.NextDate     = dto.NextDate.Value;
        if (dto.EndDate     is not null) entry.EndDate      = dto.EndDate;
        if (dto.IsActive    is not null) entry.IsActive     = dto.IsActive.Value;

        await _repo.SaveChangesAsync(ct);
        return Map(entry);
    }

    public async Task DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entry = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Asiento recurrente no encontrado.");
        _repo.Remove(entry);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<GeneratePendingResultDto> GeneratePendingAsync(Guid orgId, CancellationToken ct = default)
    {
        var today   = DateOnly.FromDateTime(DateTime.Today);
        var pending = await _repo.GetPendingAsync(orgId, today, ct);

        // Stage all entries before committing anything: if SaveChangesAsync fails, no template is advanced
        var staged = new List<(RecurringJournalEntry Template, JournalEntry Entry, DateOnly NewDate, bool Deactivate)>();
        foreach (var template in pending)
        {
            var newDate    = Advance(template.NextDate, template.Frequency);
            var deactivate = template.EndDate.HasValue && newDate > template.EndDate.Value;

            var entry = new JournalEntry
            {
                OrganizationId = orgId,
                Date           = template.NextDate,
                Description    = template.Description,
                Reference      = template.Reference,
                Status         = JournalStatus.Draft,
                Lines          = template.Lines.Select(l => new JournalLine
                {
                    AccountId = l.AccountId,
                    Debit     = l.Debit,
                    Credit    = l.Credit,
                    Note      = l.Note,
                }).ToList(),
            };
            await _journal.AddAsync(entry, ct);
            staged.Add((template, entry, newDate, deactivate));
        }

        // Persist entries first — entries are durable before any template is advanced
        await _journal.SaveChangesAsync(ct);

        // Advance templates after entries are safe; a lost optimistic race leaves a harmless draft
        var ids = new List<Guid>();
        foreach (var (template, entry, newDate, deactivate) in staged)
        {
            var advanced = await _repo.TryAdvanceAsync(template.Id, template.NextDate, newDate, deactivate, ct);
            if (advanced) ids.Add(entry.Id);
        }

        return new GeneratePendingResultDto(ids.Count, ids);
    }

    private static DateOnly Advance(DateOnly date, RecurringFrequency freq) => freq switch
    {
        RecurringFrequency.Weekly    => date.AddDays(7),
        RecurringFrequency.Biweekly  => date.AddDays(14),
        RecurringFrequency.Monthly   => date.AddMonths(1),
        RecurringFrequency.Quarterly => date.AddMonths(3),
        RecurringFrequency.Annually  => date.AddYears(1),
        _                            => date.AddMonths(1),
    };

    private static RecurringEntryDto Map(RecurringJournalEntry e) =>
        new(e.Id, e.Description, e.Reference, e.Frequency, FrequencyLabel(e.Frequency),
            e.NextDate, e.EndDate, e.IsActive,
            e.Lines.Select(l => new RecurringLineDto(
                l.Id, l.AccountId, l.Account.Code, l.Account.Name,
                l.Debit, l.Credit, l.Note)).ToList());

    private static string FrequencyLabel(RecurringFrequency f) => f switch
    {
        RecurringFrequency.Weekly    => "Semanal",
        RecurringFrequency.Biweekly  => "Quincenal",
        RecurringFrequency.Monthly   => "Mensual",
        RecurringFrequency.Quarterly => "Trimestral",
        RecurringFrequency.Annually  => "Anual",
        _                            => f.ToString(),
    };
}
