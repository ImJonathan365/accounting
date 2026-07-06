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
        var q = _db.Invoices
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .Where(i => i.OrganizationId == orgId);

        if (type is not null)
            q = q.Where(i => i.Type == type);

        return q.OrderByDescending(i => i.Date).ToListAsync(ct);
    }

    public Task<Invoice?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Invoices
            .Include(i => i.Contact)
            .Include(i => i.ArApAccount)
            .Include(i => i.Lines).ThenInclude(l => l.Account)
            .Include(i => i.Payments).ThenInclude(p => p.PaymentAccount)
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Id == id, ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default) =>
        await _db.Invoices.AddAsync(invoice, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
