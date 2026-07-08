using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetByOrganizationAsync(Guid orgId, InvoiceType? type = null, CancellationToken ct = default);
    Task<Invoice?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<Invoice?>      GetByIdReadOnlyAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<List<Invoice>> GetOverdueAsync(Guid orgId, CancellationToken ct = default);
    Task                AddAsync(Invoice invoice, CancellationToken ct = default);
    Task                AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
