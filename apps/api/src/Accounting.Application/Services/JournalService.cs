using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IJournalService
{
    Task<PagedResult<JournalEntrySummaryDto>> ListAsync(
        Guid orgId, int page, int pageSize,
        DateOnly? from = null, DateOnly? to = null,
        JournalStatus? status = null, string? search = null,
        CancellationToken ct = default);
    Task<JournalEntryDto> GetByIdAsync(Guid id, Guid orgId, CancellationToken ct = default);
    Task<JournalEntryDto> CreateAsync(Guid orgId, CreateJournalEntryDto dto, CancellationToken ct = default);
    Task<JournalEntryDto> UpdateAsync(Guid orgId, Guid entryId, UpdateJournalEntryDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid orgId, Guid entryId, CancellationToken ct = default);
    Task<JournalEntryDto> PostAsync(Guid orgId, Guid entryId, CancellationToken ct = default);
    Task<JournalEntryDto> VoidAsync(Guid orgId, Guid entryId, VoidJournalEntryDto dto, CancellationToken ct = default);
}

public class JournalService : IJournalService
{
    private readonly IJournalRepository            _journal;
    private readonly IAccountRepository            _accounts;
    private readonly IAccountingPeriodRepository   _periods;
    private readonly IValidator<CreateJournalEntryDto> _createValidator;
    private readonly IValidator<UpdateJournalEntryDto> _updateValidator;
    private readonly IValidator<VoidJournalEntryDto>   _voidValidator;

    public JournalService(
        IJournalRepository journal,
        IAccountRepository accounts,
        IAccountingPeriodRepository periods,
        IValidator<CreateJournalEntryDto> createValidator,
        IValidator<UpdateJournalEntryDto> updateValidator,
        IValidator<VoidJournalEntryDto> voidValidator)
    {
        _journal         = journal;
        _accounts        = accounts;
        _periods         = periods;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _voidValidator   = voidValidator;
    }

    public async Task<PagedResult<JournalEntrySummaryDto>> ListAsync(
        Guid orgId, int page, int pageSize,
        DateOnly? from = null, DateOnly? to = null,
        JournalStatus? status = null, string? search = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _journal.GetPagedAsync(orgId, page, pageSize, from, to, status, search, ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<JournalEntrySummaryDto>(
            items.Select(MapSummary).ToList(),
            total, page, pageSize, totalPages);
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

        var inactive = accounts.Where(a => !a.IsActive).Select(a => a.Code).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas están inactivas: {string.Join(", ", inactive)}.");

        var nonPostable = accounts.Where(a => !a.IsPostable).Select(a => a.Code).ToList();
        if (nonPostable.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas no admiten movimientos: {string.Join(", ", nonPostable)}.");

        // Period check only for posted entries (drafts are allowed in closed periods)
        if (!dto.IsDraft)
            await AssertPeriodOpenAsync(orgId, dto.Date, ct);

        var accountMap = accounts.ToDictionary(a => a.Id);

        var entry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = dto.Date,
            Description    = dto.Description.Trim(),
            Reference      = dto.Reference?.Trim(),
            Status         = dto.IsDraft ? JournalStatus.Draft : JournalStatus.Posted,
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

    public async Task<JournalEntryDto> UpdateAsync(
        Guid orgId, Guid entryId, UpdateJournalEntryDto dto, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, ct);

        var entry = await _journal.GetByIdTrackedAsync(entryId, orgId, ct)
            ?? throw new KeyNotFoundException($"Asiento {entryId} no encontrado.");

        if (entry.Status != JournalStatus.Draft)
            throw new InvalidOperationException("Solo se pueden editar asientos en estado Borrador.");

        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts   = await _accounts.GetByIdsAsync(orgId, accountIds, ct);

        if (accounts.Count != accountIds.Count)
            throw new InvalidOperationException("Una o más cuentas no existen en esta organización.");

        var inactive = accounts.Where(a => !a.IsActive).Select(a => a.Code).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas están inactivas: {string.Join(", ", inactive)}.");

        var nonPostable = accounts.Where(a => !a.IsPostable).Select(a => a.Code).ToList();
        if (nonPostable.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas no admiten movimientos: {string.Join(", ", nonPostable)}.");

        var accountMap = accounts.ToDictionary(a => a.Id);

        entry.Date        = dto.Date;
        entry.Description = dto.Description.Trim();
        entry.Reference   = dto.Reference?.Trim();

        entry.Lines.Clear();
        foreach (var l in dto.Lines)
            entry.Lines.Add(new JournalLine
            {
                AccountId = l.AccountId,
                Debit     = l.Debit,
                Credit    = l.Credit,
                Note      = l.Note?.Trim()
            });

        await _journal.SaveChangesAsync(ct);

        foreach (var line in entry.Lines)
            line.Account = accountMap[line.AccountId];

        return MapDetail(entry);
    }

    public async Task DeleteAsync(Guid orgId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await _journal.GetByIdTrackedAsync(entryId, orgId, ct)
            ?? throw new KeyNotFoundException($"Asiento {entryId} no encontrado.");

        if (entry.Status != JournalStatus.Draft)
            throw new InvalidOperationException("Solo se pueden eliminar asientos en estado Borrador.");

        _journal.Delete(entry);
        await _journal.SaveChangesAsync(ct);
    }

    public async Task<JournalEntryDto> PostAsync(
        Guid orgId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await _journal.GetByIdTrackedAsync(entryId, orgId, ct)
            ?? throw new KeyNotFoundException($"Asiento {entryId} no encontrado.");

        if (entry.Status != JournalStatus.Draft)
            throw new InvalidOperationException("Solo se pueden registrar asientos en estado Borrador.");

        await AssertPeriodOpenAsync(orgId, entry.Date, ct);

        var totalDebit  = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);

        if (totalDebit == 0)
            throw new InvalidOperationException("El asiento no tiene montos. Completa las líneas antes de registrar.");

        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                $"El asiento no está balanceado: débitos {totalDebit:F2} ≠ créditos {totalCredit:F2}.");

        var inactive = entry.Lines
            .Where(l => !l.Account.IsActive).Select(l => l.Account.Code).Distinct().ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas están inactivas: {string.Join(", ", inactive)}.");

        var nonPostable = entry.Lines
            .Where(l => !l.Account.IsPostable).Select(l => l.Account.Code).Distinct().ToList();
        if (nonPostable.Count > 0)
            throw new InvalidOperationException(
                $"Las siguientes cuentas no admiten movimientos: {string.Join(", ", nonPostable)}.");

        entry.Status = JournalStatus.Posted;
        await _journal.SaveChangesAsync(ct);

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

        await AssertPeriodOpenAsync(orgId, voidDate, ct);

        // Build a human-readable reference for the counter-entry
        var baseRef = original.Reference ?? original.Id.ToString("N")[..8];
        var rawRef  = $"ANULA/{baseRef}";
        var voidRef = rawRef.Length > 100 ? rawRef[..100] : rawRef;

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

        original.Status          = JournalStatus.Voided;
        original.VoidReason      = dto.Reason?.Trim();
        original.VoidedAtUtc     = DateTime.UtcNow;
        original.VoidedByEntryId = counterEntry.Id;

        await _journal.AddAsync(counterEntry, ct);
        await _journal.SaveChangesAsync(ct);

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

    private async Task AssertPeriodOpenAsync(Guid orgId, DateOnly date, CancellationToken ct)
    {
        if (await _periods.IsClosedAsync(orgId, date.Year, date.Month, ct))
            throw new InvalidOperationException(
                $"El período {date:MMMM yyyy} está cerrado. Reabre el período para registrar asientos en esa fecha.");
    }
}
