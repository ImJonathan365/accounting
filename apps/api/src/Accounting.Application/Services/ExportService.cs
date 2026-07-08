using Accounting.Application.DTOs;

namespace Accounting.Application.Services;

public interface IExportService
{
    Task<ExportResult> ExportTrialBalanceAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default);

    Task<ExportResult> ExportIncomeStatementAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default);

    Task<ExportResult> ExportBalanceSheetAsync(
        Guid orgId, DateOnly asOf, string format, CancellationToken ct = default);

    Task<ExportResult> ExportCashFlowAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default);

    byte[] GenerateInvoicePdf(InvoiceDto invoice, OrgSettingsDto settings);
}
