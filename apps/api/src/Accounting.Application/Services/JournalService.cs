using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IJournalService
{
    Task<List<JournalEntrySummaryDto>> ListAsync(Guid orgId, CancellationToken ct = default);
    Task<JournalEntryDto> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default);
    Task<JournalEntryDto> CreateAsync(Guid orgId, CreateJournalEntryDto dto, CancellationToken ct = default);
    Task<JournalEntryDto> VoidAsync(Guid orgId, Guid entryId, VoidJournalEntryDto dto, CancellationToken ct = default);
}

public class JournalService : IJournalService
{
    private readonly IJournalRepository _journal;
    private readonly IAccountRepository _accounts;
    private readonly IValidator<CreateJournalEntryDto> _createValidator;
    private readonly IValidator<VoidJournalEntryDto>   _voidValidator;

    public JournalService(
        IJournalRepository journal,
        IAccountRepository accounts,
        IValidator<CreateJournalEntryDto> createValidator,
        IValidator<VoidJournalEntryDto> voidValidator)
    {
        _journal        = journal;
        _accounts       = accounts;
        _createValidator = createValidator;
        _voidValidator   = voidValidator;
    }

    public async Task<List<JournalEntrySummaryDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var entries = await _journal.GetByOrganizationAsync(orgId, ct);
        return entries.Select(MapSummary).ToList();
    }

    public async Task<JournalEntryDto> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default)
    {
        var entry = await _journal.GetByIdAsync(id, orgId, ct)
            ?? throw new KeyNotFoundException($"Asiento {id} no encontrado.");
        return MapDetail(entry);
    }

    public async Task<JournalEntryDto> CreateAsync(Guid orgId, CreateJournalEntryDto dto, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, ct);

        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _accounts.GetByIdsAsync(orgId, accountIds, ct);

        if (accounts.Count != accountIds.Count)
            throw new InvalidOperationException("Una o más cuentas no existen en esta organización.");

        var nonPostable = accounts.Where(a => !a.IsPostable).Select(a => a.Code).ToList();
        if (nonPostable.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas no admiten movimientos: {string.Join(", ", nonPostable)}.");

        var accountMap = accounts.ToDictionary(a => a.Id);

        var entry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = dto.Date,
            Description    = dto.Description.Trim(),
            Reference      = dto.Reference?.Trim(),
            Status         = JournalStatus.Posted,
            Lines          = dto.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit     = l.Debit,
                Credit    = l.Credit,
                Note      = l.Note?.Trim()
            }).ToList()
        };

        await _journal.AddAsync(entry, ct);
        await _journal.SaveChangesAsync(ct);

        foreach (var line in entry.Lines)
            line.Account = accountMap[line.AccountId];

        return MapDetail(entry);
    }

    public async Task<JournalEntryDto> VoidAsync(
        Guid orgId, Guid entryId, VoidJournalEntryDto dto, CancellationToken ct = default)
    {
        await _voidValidator.ValidateAndThrowAsync(dto, ct);

        var original = await _journal.GetByIdTrackedAsync(entryId, orgId, ct)
            ?? throw new KeyNotFoundException($"Asiento {entryId} no encontrado.");

        if (original.Status == JournalStatus.Voided)
            throw new InvalidOperationException("Este asiento ya fue anulado.");

        if (original.Status != JournalStatus.Posted)
            throw new InvalidOperationException("Solo se pueden anular asientos en estado Registrado.");

        var voidDate = dto.VoidDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Build a human-readable reference for the counter-entry
        var rawRef    = original.Reference is not null ? $"ANULA/{original.Reference}" : $"ANULA/{original.Id:N}"[..14];
        var voidRef   = rawRef.Length > 100 ? rawRef[..100] : rawRef;

        // Counter-entry reverses every line (debit ↔ credit)
        var counterEntry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = voidDate,
            Description    = $"ANULACIÓN: {original.Description}",
            Reference      = voidRef,
            Status         = JournalStatus.Posted,
            VoidsEntryId   = original.Id,
            Lines          = original.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit     = l.Credit,
                Credit    = l.Debit,
                Note      = "Anulación"
            }).ToList()
        };

        // Mark original as voided — reference the counter-entry by its pre-assigned Id
        original.Status          = JournalStatus.Voided;
        original.VoidReason      = dto.Reason?.Trim();
        original.VoidedAtUtc     = DateTime.UtcNow;
        original.VoidedByEntryId = counterEntry.Id;

        await _journal.AddAsync(counterEntry, ct);
        await _journal.SaveChangesAsync(ct);   // Single transaction: both changes committed together

        return MapDetail(original);
    }

    private static JournalEntrySummaryDto MapSummary(JournalEntry e) =>
        new(e.Id, e.Date, e.Description, e.Reference, e.Status,
            e.Lines.Sum(l => l.Debit), e.CreatedAtUtc,
            e.VoidsEntryId, e.VoidedByEntryId);

    private static JournalEntryDto MapDetail(JournalEntry e) =>
        new(e.Id, e.Date, e.Description, e.Reference, e.Status,
            e.Lines.Sum(l => l.Debit), e.Lines.Sum(l => l.Credit),
            e.Lines.Select(l => new JournalLineDto(
                l.Id, l.AccountId, l.Account.Code, l.Account.Name,
                l.Debit, l.Credit, l.Note)).ToList(),
            e.CreatedAtUtc,
            e.VoidsEntryId, e.VoidedByEntryId, e.VoidReason, e.VoidedAtUtc);
}
