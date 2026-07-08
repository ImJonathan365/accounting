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

    public Task<List<Invoice>> GetOverdueAsync(Guid orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _db.Invoices.AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.Lines).ThenInclude(l => l.TaxRate)
            .Include(i => i.Payments)
            .Where(i => i.OrganizationId == orgId
                     && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                     && i.DueDate < today)
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
