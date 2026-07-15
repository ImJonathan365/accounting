using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>>  GetAllAsync(Guid orgId, InvoiceType? type, InvoiceStatus? status, string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
    Task<InvoiceDto>               GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<List<OverdueInvoiceDto>>  GetOverdueAsync(Guid orgId, CancellationToken ct = default);
    Task<InvoiceDto>               CreateAsync(Guid orgId, CreateInvoiceDto dto, CancellationToken ct = default);
    Task<InvoiceDto>               IssueAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<InvoiceDto>               RecordPaymentAsync(Guid orgId, Guid id, CreatePaymentDto dto, CancellationToken ct = default);
    Task<InvoiceDto>               VoidAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository          _repo;
    private readonly IContactRepository          _contacts;
    private readonly IAccountRepository          _accounts;
    private readonly IJournalRepository          _journal;
    private readonly ITaxRateRepository          _taxRates;
    private readonly IJournalService             _journalSvc;
    private readonly IAccountingPeriodRepository _periods;

    public InvoiceService(
        IInvoiceRepository          repo,
        IContactRepository          contacts,
        IAccountRepository          accounts,
        IJournalRepository          journal,
        ITaxRateRepository          taxRates,
        IJournalService             journalSvc,
        IAccountingPeriodRepository periods)
    {
        _repo       = repo;
        _contacts   = contacts;
        _accounts   = accounts;
        _journal    = journal;
        _taxRates   = taxRates;
        _journalSvc = journalSvc;
        _periods    = periods;
    }

    public async Task<PagedResult<InvoiceDto>> GetAllAsync(
        Guid orgId, InvoiceType? type, InvoiceStatus? status, string? search,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetPagedAsync(orgId, type, status, search, from, to, page, pageSize, ct);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<InvoiceDto>(items.Select(Map).ToList(), total, page, pageSize, totalPages);
    }

    public async Task<InvoiceDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdReadOnlyAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");
        return Map(inv);
    }

    public async Task<List<OverdueInvoiceDto>> GetOverdueAsync(Guid orgId, CancellationToken ct = default)
    {
        var list = await _repo.GetOverdueAsync(orgId, ct);
        return list.Select(i => new OverdueInvoiceDto(
            i.Id, i.Number, i.ContactName,
            i.DueDate.ToString("yyyy-MM-dd"),
            i.Type.ToString(),
            i.Balance
        )).ToList();
    }

    public async Task<InvoiceDto> CreateAsync(Guid orgId, CreateInvoiceDto dto, CancellationToken ct = default)
    {
        if (!dto.Lines.Any())
            throw new ArgumentException("La factura debe tener al menos una línea.");

        if (await _repo.NumberExistsAsync(orgId, dto.Number.Trim(), null, ct))
            throw new InvalidOperationException($"Ya existe una factura con el número '{dto.Number.Trim()}' en esta organización.");

        var contact = await _contacts.GetByIdAsync(orgId, dto.ContactId, ct);
        if (contact is null)
            throw new ArgumentException("El contacto no pertenece a esta organización.");

        var compatible = dto.Type == InvoiceType.Receivable
            ? contact.Type is ContactType.Customer or ContactType.Both
            : contact.Type is ContactType.Vendor or ContactType.Both;
        if (!compatible)
        {
            var expected = dto.Type == InvoiceType.Receivable ? "cliente" : "proveedor";
            throw new ArgumentException($"El contacto debe ser de tipo {expected} para este tipo de factura.");
        }

        var accountIds = dto.Lines.Select(l => l.AccountId).Append(dto.ArApAccountId).Distinct().ToList();
        var accounts   = await _accounts.GetByIdsAsync(orgId, accountIds, ct);
        if (accounts.Count != accountIds.Count)
            throw new ArgumentException("Una o más cuentas no pertenecen a la organización.");

        var invoice = new Invoice
        {
            OrganizationId = orgId,
            Type           = dto.Type,
            ContactId      = dto.ContactId,
            Number         = dto.Number.Trim(),
            Date           = DateOnly.Parse(dto.Date),
            DueDate        = DateOnly.Parse(dto.DueDate),
            ArApAccountId  = dto.ArApAccountId,
            Notes          = dto.Notes?.Trim(),
            Lines = dto.Lines.Select(l => new InvoiceLine
            {
                Description = l.Description.Trim(),
                Quantity    = l.Quantity,
                UnitPrice   = l.UnitPrice,
                AccountId   = l.AccountId,
                TaxRateId   = l.TaxRateId,
            }).ToList(),
        };

        await _repo.AddAsync(invoice, ct);
        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdReadOnlyAsync(orgId, invoice.Id, ct);
        return Map(full!);
    }

    public async Task<InvoiceDto> IssueAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Solo se pueden emitir facturas en estado borrador.");

        await AssertPeriodOpenAsync(orgId, inv.Date, ct);

        // Load tax rate data separately (AsNoTracking) to avoid EF change-tracker side-effects
        var taxRateIds = inv.Lines.Where(l => l.TaxRateId.HasValue).Select(l => l.TaxRateId!.Value).ToList();
        var taxRateMap = taxRateIds.Count > 0
            ? (await _taxRates.GetByIdsAsync(taxRateIds, orgId, ct)).ToDictionary(t => t.Id)
            : new Dictionary<Guid, TaxRate>();

        // M-9: all TaxRateIds must belong to the org
        var missingRates = taxRateIds.Except(taxRateMap.Keys).ToList();
        if (missingRates.Count > 0)
            throw new InvalidOperationException("Una o más tasas de impuesto en las líneas no pertenecen a esta organización.");

        // M-8: re-validate accounts at issuance time (they may have changed since draft creation)
        var allAccountIds = inv.Lines.Select(l => l.AccountId).Append(inv.ArApAccountId).Distinct().ToList();
        var allAccounts   = await _accounts.GetByIdsAsync(orgId, allAccountIds, ct);
        var inactive      = allAccounts.Where(a => !a.IsActive).Select(a => a.Code).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException($"Las siguientes cuentas están inactivas: {string.Join(", ", inactive)}.");
        var nonPostable = allAccounts.Where(a => !a.IsPostable).Select(a => a.Code).ToList();
        if (nonPostable.Count > 0)
            throw new InvalidOperationException($"Las siguientes cuentas no admiten movimientos: {string.Join(", ", nonPostable)}.");

        var subTotal = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var taxTotal = inv.Lines.Sum(l =>
        {
            var rate = l.TaxRateId.HasValue && taxRateMap.TryGetValue(l.TaxRateId.Value, out var tr) ? tr.Rate : 0m;
            return l.Quantity * l.UnitPrice * rate / 100;
        });
        var total = subTotal + taxTotal;

        // Build journal entry: AR/AP account vs income/expense + tax accounts
        var lines = new List<JournalLine>();
        if (inv.Type == InvoiceType.Receivable)
        {
            lines.Add(new JournalLine { AccountId = inv.ArApAccountId, Debit = total, Credit = 0 });
            foreach (var l in inv.Lines)
                lines.Add(new JournalLine { AccountId = l.AccountId, Debit = 0, Credit = l.Quantity * l.UnitPrice });
        }
        else
        {
            lines.Add(new JournalLine { AccountId = inv.ArApAccountId, Debit = 0, Credit = total });
            foreach (var l in inv.Lines)
                lines.Add(new JournalLine { AccountId = l.AccountId, Debit = l.Quantity * l.UnitPrice, Credit = 0 });
        }

        // Tax lines grouped by tax account
        var taxGroups = inv.Lines
            .Where(l => l.TaxRateId.HasValue && taxRateMap.ContainsKey(l.TaxRateId!.Value))
            .GroupBy(l => taxRateMap[l.TaxRateId!.Value].TaxAccountId);

        foreach (var g in taxGroups)
        {
            var taxAmt = g.Sum(l => l.Quantity * l.UnitPrice * taxRateMap[l.TaxRateId!.Value].Rate / 100);
            if (taxAmt == 0) continue;
            if (inv.Type == InvoiceType.Receivable)
                lines.Add(new JournalLine { AccountId = g.Key, Debit = 0, Credit = taxAmt });
            else
                lines.Add(new JournalLine { AccountId = g.Key, Debit = taxAmt, Credit = 0 });
        }

        var entry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = inv.Date,
            Reference      = inv.Number,
            Description    = $"Factura {inv.Number} — {inv.Contact.Name}",
            Status         = JournalStatus.Posted,
            Lines          = lines,
        };

        await _journal.AddAsync(entry, ct);
        inv.JournalEntryId = entry.Id;
        inv.Status         = InvoiceStatus.Issued;
        await _repo.SaveChangesAsync(ct);

        return await GetByIdAsync(orgId, id, ct);
    }

    public async Task<InvoiceDto> RecordPaymentAsync(Guid orgId, Guid id, CreatePaymentDto dto, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        if (inv.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
            throw new InvalidOperationException("La factura debe estar emitida para registrar pagos.");

        var paymentDate = DateOnly.Parse(dto.Date);
        await AssertPeriodOpenAsync(orgId, paymentDate, ct);

        if (await _accounts.GetByIdAsync(dto.PaymentAccountId, orgId, ct) is null)
            throw new ArgumentException("La cuenta de pago no pertenece a esta organización.");

        // Load tax rates separately (AsNoTracking) to compute the correct tax-inclusive total
        var taxRateIds = inv.Lines.Where(l => l.TaxRateId.HasValue).Select(l => l.TaxRateId!.Value).ToList();
        var taxRateMap = taxRateIds.Count > 0
            ? (await _taxRates.GetByIdsAsync(taxRateIds, orgId, ct)).ToDictionary(t => t.Id, t => t.Rate)
            : new Dictionary<Guid, decimal>();

        var total = inv.Lines.Sum(l =>
        {
            var rate = l.TaxRateId.HasValue && taxRateMap.TryGetValue(l.TaxRateId.Value, out var r) ? r : 0m;
            return l.Quantity * l.UnitPrice * (1 + rate / 100);
        });
        var paid    = inv.Payments.Sum(p => p.Amount);
        var balance = total - paid;

        if (dto.Amount <= 0 || dto.Amount > balance + 0.001m)
            throw new ArgumentException($"El monto del pago debe ser entre 0.01 y {balance:F2}.");

        // Journal entry for payment
        var lines = inv.Type == InvoiceType.Receivable
            ? new List<JournalLine>
            {
                new() { AccountId = dto.PaymentAccountId, Debit = dto.Amount, Credit = 0 },
                new() { AccountId = inv.ArApAccountId,    Debit = 0, Credit = dto.Amount },
            }
            : new List<JournalLine>
            {
                new() { AccountId = inv.ArApAccountId,    Debit = dto.Amount, Credit = 0 },
                new() { AccountId = dto.PaymentAccountId, Debit = 0, Credit = dto.Amount },
            };

        var entry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = paymentDate,
            Reference      = inv.Number,
            Description    = $"Pago factura {inv.Number} — {inv.Contact.Name}",
            Status         = JournalStatus.Posted,
            Lines          = lines,
        };

        await _journal.AddAsync(entry, ct);

        var payment = new InvoicePayment
        {
            InvoiceId        = inv.Id,
            Date             = paymentDate,
            Amount           = dto.Amount,
            PaymentAccountId = dto.PaymentAccountId,
            JournalEntryId   = entry.Id,
            Notes            = dto.Notes?.Trim(),
        };
        // Use AddPaymentAsync (direct DbSet insert) instead of collection nav to guarantee
        // Added state — EF Core 10 can mistrack the entity as Modified via collection fixup
        await _repo.AddPaymentAsync(payment, ct);

        var newPaid = paid + dto.Amount;
        inv.Status = Math.Abs(newPaid - total) < 0.005m
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        await _repo.SaveChangesAsync(ct);
        return await GetByIdAsync(orgId, id, ct);
    }

    public async Task<InvoiceDto> VoidAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        if (inv.Status == InvoiceStatus.Void)
            throw new InvalidOperationException("La factura ya está anulada.");

        if (inv.Payments.Any())
            throw new InvalidOperationException("No se puede anular una factura con pagos registrados.");

        inv.Status = InvoiceStatus.Void;

        if (inv.JournalEntryId.HasValue)
        {
            // VoidAsync saves all pending changes (including the status above) via shared DbContext
            await _journalSvc.VoidAsync(orgId, inv.JournalEntryId.Value,
                new VoidJournalEntryDto($"Factura anulada: {inv.Number}", null), ct);
        }
        else
        {
            await _repo.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(orgId, id, ct);
    }

    private async Task AssertPeriodOpenAsync(Guid orgId, DateOnly date, CancellationToken ct)
    {
        if (await _periods.IsClosedAsync(orgId, date.Year, date.Month, ct))
            throw new InvalidOperationException(
                $"El período {date:MMMM yyyy} está cerrado. No se pueden registrar movimientos en períodos cerrados.");
    }

    private static string StatusLabel(InvoiceStatus s) => s switch
    {
        InvoiceStatus.Draft         => "Borrador",
        InvoiceStatus.Issued        => "Emitida",
        InvoiceStatus.PartiallyPaid => "Parcialmente pagada",
        InvoiceStatus.Paid          => "Pagada",
        InvoiceStatus.Void          => "Anulada",
        _                           => s.ToString(),
    };

    private static InvoiceDto Map(Invoice inv)
    {
        var subTotal = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var taxTotal = inv.Lines.Sum(l => l.Quantity * l.UnitPrice * (l.TaxRate?.Rate ?? 0) / 100);
        var total    = subTotal + taxTotal;
        var paid     = inv.Payments.Sum(p => p.Amount);
        return new InvoiceDto(
            inv.Id, inv.Type,
            inv.ContactId, inv.Contact?.Name ?? "",
            inv.Number,
            inv.Date.ToString("yyyy-MM-dd"),
            inv.DueDate.ToString("yyyy-MM-dd"),
            inv.Status, StatusLabel(inv.Status),
            inv.ArApAccountId, inv.ArApAccount?.Name ?? "",
            inv.Notes, inv.JournalEntryId,
            subTotal, taxTotal, total, paid, total - paid,
            inv.Lines.Select(l =>
            {
                var sub    = l.Quantity * l.UnitPrice;
                var taxPct = l.TaxRate?.Rate ?? 0;
                var taxAmt = sub * taxPct / 100;
                return new InvoiceLineDto(
                    l.Id, l.Description, l.Quantity, l.UnitPrice, sub,
                    l.AccountId, l.Account?.Code ?? "", l.Account?.Name ?? "",
                    l.TaxRateId, l.TaxRate?.Name, taxPct, taxAmt, sub + taxAmt);
            }).ToList(),
            inv.Payments.Select(p => new InvoicePaymentDto(
                p.Id, p.Date.ToString("yyyy-MM-dd"), p.Amount,
                p.PaymentAccountId, p.PaymentAccount?.Name ?? "",
                p.JournalEntryId, p.Notes)).ToList());
    }
}
