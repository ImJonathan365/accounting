using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;
    public InvoiceRepository(AppDbContext db) => _db = db;

    public Task<List<Invoice>> GetByOrganizationAsync(Guid orgId, InvoiceType? type = null, CancellationToken ct = default)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Lines).ThenInclude(l => l.TaxRate)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .Where(i => i.OrganizationId == orgId);

        if (type is not null)
            q = q.Where(i => i.Type == type);

        return q.OrderByDescending(i => i.Date).ToListAsync(ct);
    }

    public async Task<(List<Invoice> Items, int Total)> GetPagedAsync(
        Guid orgId, InvoiceType? type, InvoiceStatus? status, string? search,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Lines).ThenInclude(l => l.TaxRate)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .Where(i => i.OrganizationId == orgId);

        if (type   is not null) q = q.Where(i => i.Type   == type);
        if (status is not null) q = q.Where(i => i.Status == status);
        if (from   is not null) q = q.Where(i => i.Date   >= from);
        if (to     is not null) q = q.Where(i => i.Date   <= to);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim().Replace("%", "\\%").Replace("_", "\\_");
            var term    = $"%{escaped}%";
            q = q.Where(i => EF.Functions.ILike(i.Number, term, "\\")
                           || (i.Contact != null && EF.Functions.ILike(i.Contact.Name, term, "\\")));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(i => i.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<List<ActiveInvoiceSummary>> GetActiveForDashboardAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Invoices.AsNoTracking()
            .Where(i => i.OrganizationId == orgId
                     && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid))
            .Select(i => new ActiveInvoiceSummary(
                i.Id,
                i.Number,
                i.Contact != null ? i.Contact.Name : "",
                i.Type,
                i.DueDate,
                i.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + (l.TaxRate != null ? l.TaxRate.Rate : 0m) / 100m))
                    - i.Payments.Sum(p => p.Amount)))
            .ToListAsync(ct);

    public Task<bool> NumberExistsAsync(Guid orgId, string number, Guid? excludeId, CancellationToken ct = default) =>
        _db.Invoices.AnyAsync(i =>
            i.OrganizationId == orgId &&
            i.Number == number &&
            (excludeId == null || i.Id != excludeId), ct);

    public Task<List<ActiveInvoiceSummary>> GetOverdueAsync(Guid orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _db.Invoices.AsNoTracking()
            .Where(i => i.OrganizationId == orgId
                     && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                     && i.DueDate < today)
            .Select(i => new ActiveInvoiceSummary(
                i.Id,
                i.Number,
                i.Contact != null ? i.Contact.Name : "",
                i.Type,
                i.DueDate,
                i.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + (l.TaxRate != null ? l.TaxRate.Rate : 0m) / 100m))
                    - i.Payments.Sum(p => p.Amount)))
            .OrderBy(i => i.DueDate)
            .ToListAsync(ct);
    }

    // Mutation path: no TaxRate include to avoid EF change tracker side-effects
    public Task<Invoice?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Invoices
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Id == id, ct);

    // Read-only path: includes TaxRate for display purposes
    public Task<Invoice?> GetByIdReadOnlyAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Invoices.AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Lines).ThenInclude(l => l.TaxRate)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Id == id, ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default) =>
        await _db.Invoices.AddAsync(invoice, ct);

    // Adds the payment directly to the DbSet (not via collection nav) to guarantee Added state
    public async Task AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default) =>
        await _db.InvoicePayments.AddAsync(payment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
