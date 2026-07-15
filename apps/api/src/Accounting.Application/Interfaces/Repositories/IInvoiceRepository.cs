using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<List<Invoice>>             GetByOrganizationAsync(Guid orgId, InvoiceType? type = null, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> GetPagedAsync(Guid orgId, InvoiceType? type, InvoiceStatus? status, string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
    Task<Invoice?>                  GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<Invoice?>                  GetByIdReadOnlyAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<List<ActiveInvoiceSummary>> GetOverdueAsync(Guid orgId, CancellationToken ct = default);
    Task<List<ActiveInvoiceSummary>> GetActiveForDashboardAsync(Guid orgId, CancellationToken ct = default);
    Task<bool>                      NumberExistsAsync(Guid orgId, string number, Guid? excludeId, CancellationToken ct = default);
    Task                            AddAsync(Invoice invoice, CancellationToken ct = default);
    Task                            AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default);
    Task                            SaveChangesAsync(CancellationToken ct = default);
}

public record ActiveInvoiceSummary(
    Guid         Id,
    string       Number,
    string       ContactName,
    InvoiceType  Type,
    DateOnly     DueDate,
    decimal      Balance);
