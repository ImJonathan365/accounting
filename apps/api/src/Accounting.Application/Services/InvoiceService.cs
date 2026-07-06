using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetAllAsync(Guid orgId, InvoiceType? type = null, CancellationToken ct = default);
    Task<InvoiceDto>       GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<InvoiceDto>       CreateAsync(Guid orgId, CreateInvoiceDto dto, CancellationToken ct = default);
    Task<InvoiceDto>       IssueAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<InvoiceDto>       RecordPaymentAsync(Guid orgId, Guid id, CreatePaymentDto dto, CancellationToken ct = default);
    Task<InvoiceDto>       VoidAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly IAccountRepository _accounts;
    private readonly IJournalRepository _journal;

    public InvoiceService(IInvoiceRepository repo, IAccountRepository accounts, IJournalRepository journal)
    {
        _repo     = repo;
        _accounts = accounts;
        _journal  = journal;
    }

    public async Task<List<InvoiceDto>> GetAllAsync(Guid orgId, InvoiceType? type = null, CancellationToken ct = default)
    {
        var list = await _repo.GetByOrganizationAsync(orgId, type, ct);
        return list.Select(Map).ToList();
    }

    public async Task<InvoiceDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");
        return Map(inv);
    }

    public async Task<InvoiceDto> CreateAsync(Guid orgId, CreateInvoiceDto dto, CancellationToken ct = default)
    {
        if (!dto.Lines.Any())
            throw new ArgumentException("La factura debe tener al menos una línea.");

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
            }).ToList(),
        };

        await _repo.AddAsync(invoice, ct);
        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdAsync(orgId, invoice.Id, ct);
        return Map(full!);
    }

    public async Task<InvoiceDto> IssueAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Solo se pueden emitir facturas en estado borrador.");

        var total = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);

        // Build journal entry: AR/AP account vs income/expense accounts
        var lines = new List<JournalLine>();
        if (inv.Type == InvoiceType.Receivable)
        {
            lines.Add(new JournalLine { AccountId = inv.ArApAccountId, Debit = total, Credit = 0 });
            foreach (var l in inv.Lines)
            {
                var sub = l.Quantity * l.UnitPrice;
                lines.Add(new JournalLine { AccountId = l.AccountId, Debit = 0, Credit = sub });
            }
        }
        else
        {
            lines.Add(new JournalLine { AccountId = inv.ArApAccountId, Debit = 0, Credit = total });
            foreach (var l in inv.Lines)
            {
                var sub = l.Quantity * l.UnitPrice;
                lines.Add(new JournalLine { AccountId = l.AccountId, Debit = sub, Credit = 0 });
            }
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

        return Map(inv);
    }

    public async Task<InvoiceDto> RecordPaymentAsync(Guid orgId, Guid id, CreatePaymentDto dto, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Factura no encontrada.");

        if (inv.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
            throw new InvalidOperationException("La factura debe estar emitida para registrar pagos.");

        var total   = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var paid    = inv.Payments.Sum(p => p.Amount);
        var balance = total - paid;

        if (dto.Amount <= 0 || dto.Amount > balance)
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
            Date           = DateOnly.Parse(dto.Date),
            Reference      = inv.Number,
            Description    = $"Pago factura {inv.Number} — {inv.Contact.Name}",
            Status         = JournalStatus.Posted,
            Lines          = lines,
        };

        await _journal.AddAsync(entry, ct);

        var payment = new InvoicePayment
        {
            InvoiceId        = inv.Id,
            Date             = DateOnly.Parse(dto.Date),
            Amount           = dto.Amount,
            PaymentAccountId = dto.PaymentAccountId,
            JournalEntryId   = entry.Id,
            Notes            = dto.Notes?.Trim(),
        };
        inv.Payments.Add(payment);

        var newPaid = paid + dto.Amount;
        inv.Status = Math.Abs(newPaid - total) < 0.001m
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        await _repo.SaveChangesAsync(ct);
        return Map(inv);
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
        await _repo.SaveChangesAsync(ct);
        return Map(inv);
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
        var total   = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var paid    = inv.Payments.Sum(p => p.Amount);
        return new InvoiceDto(
            inv.Id, inv.Type,
            inv.ContactId, inv.Contact?.Name ?? "",
            inv.Number,
            inv.Date.ToString("yyyy-MM-dd"),
            inv.DueDate.ToString("yyyy-MM-dd"),
            inv.Status, StatusLabel(inv.Status),
            inv.ArApAccountId, inv.ArApAccount?.Name ?? "",
            inv.Notes, inv.JournalEntryId,
            total, paid, total - paid,
            inv.Lines.Select(l => new InvoiceLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.AccountId, l.Account?.Code ?? "", l.Account?.Name ?? "")).ToList(),
            inv.Payments.Select(p => new InvoicePaymentDto(
                p.Id, p.Date.ToString("yyyy-MM-dd"), p.Amount,
                p.PaymentAccountId, p.PaymentAccount?.Name ?? "",
                p.JournalEntryId, p.Notes)).ToList());
    }
}
